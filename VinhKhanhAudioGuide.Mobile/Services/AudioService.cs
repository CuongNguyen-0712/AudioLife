namespace VinhKhanhAudioGuide.Mobile.Services;

using Plugin.Maui.Audio;

/// <summary>
/// Audio playback service backed by Plugin.Maui.Audio.
/// Includes cooldown mechanism to prevent rapid successive plays (spam prevention).
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private static readonly HttpClient HttpClient = new();
    private const double DefaultPlayCooldownSeconds = 2.5;

    private readonly IAudioManager _audioManager;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly object _loadSync = new();
    private IAudioPlayer? _player;
    private Stream? _sourceStream;
    private CancellationTokenSource? _activeLoadCts;

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isPlaying;
    private double _volume = 1.0;
    private AudioPlaybackState _state = AudioPlaybackState.None;
    private string? _currentAudioUrl;
    private string? _currentLocationId;
    private string? _currentAudioGuideId;
    private IDispatcherTimer? _timer;
    private long _playRequestVersion;
    private DateTime _lastPlayAttemptUtc = DateTime.MinValue;
    private readonly TimeSpan _playCooldown = TimeSpan.FromSeconds(DefaultPlayCooldownSeconds);

    public TimeSpan CurrentPosition => _currentPosition;
    public TimeSpan Duration => _duration;
    public bool IsPlaying => _isPlaying;
    public double Volume => _volume;
    public string? CurrentAudioUrl => _currentAudioUrl;
    public string? CurrentLocationId => _currentLocationId;
    public string? CurrentAudioGuideId => _currentAudioGuideId;
    public DateTime LastPlayAttemptUtc => _lastPlayAttemptUtc;
    public TimeSpan PlayCooldown => _playCooldown;

    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioPositionChangedEventArgs>? PositionChanged;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlayAsync(string audioUrl)
    {
        await PlayAsync(audioUrl, string.Empty, string.Empty);
    }

    public async Task PlayAsync(string audioUrl, string locationId, string audioGuideId)
    {
        // Hàm phát audio chính: chặn spam bằng cooldown, load player mới và cập nhật state sự kiện.
        // Thuộc flow manual play, auto-play POI và resume playback.
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            SetState(AudioPlaybackState.Error, audioUrl);
            return;
        }

        // Apply cooldown: prevent rapid successive play attempts (spam prevention).
        var timeSinceLastAttempt = DateTime.UtcNow - _lastPlayAttemptUtc;
        if (timeSinceLastAttempt < _playCooldown)
        {
            return; // Cooldown active, ignore this play request.
        }

        _lastPlayAttemptUtc = DateTime.UtcNow;
        var requestVersion = Interlocked.Increment(ref _playRequestVersion);
        var loadCts = ReplaceActiveLoadCts();
        await _operationLock.WaitAsync();

        try
        {
            SetState(AudioPlaybackState.Loading, audioUrl);

            // Always dispose old player first to avoid overlap when users switch quickly.
            StopPositionTimer();
            CleanupPlayer();
            _isPlaying = false;
            _currentPosition = TimeSpan.Zero;

            _duration = TimeSpan.Zero;

            _currentAudioUrl = audioUrl;
            await LoadPlayerAsync(audioUrl, loadCts.Token);
            if (_player == null)
            {
                throw new InvalidOperationException("Không thể khởi tạo audio player.");
            }

            // Ignore stale play requests that completed after a newer request.
            if (requestVersion != _playRequestVersion || loadCts.IsCancellationRequested)
            {
                CleanupPlayer();
                return;
            }

            _player.Volume = _volume;
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();

            _duration = TimeSpan.FromSeconds(Math.Max(0, _player.Duration));
            _currentPosition = TimeSpan.Zero;
            _isPlaying = true;

            SetState(AudioPlaybackState.Playing, _currentAudioUrl);
            StartPositionTimer();
            RaisePositionChanged();
        }
        catch (OperationCanceledException)
        {
            CleanupPlayer();
        }
        catch
        {
            CleanupPlayer();
            _isPlaying = false;
            _currentPosition = TimeSpan.Zero;
            _duration = TimeSpan.Zero;
            _currentLocationId = null;
            _currentAudioGuideId = null;
            SetState(AudioPlaybackState.Error, _currentAudioUrl);
        }
        finally
        {
            ClearActiveLoadCts(loadCts);
            loadCts.Dispose();
            _operationLock.Release();
        }
    }

    public async Task PauseAsync()
    {
        // Tạm dừng audio hiện tại và giữ lại vị trí để resume.
        // Thuộc flow điều khiển player từ AudioPlayerViewModel.
        await _operationLock.WaitAsync();
        try
        {
            if (!_isPlaying || _player == null)
            {
                return;
            }

            _player.Pause();
            _isPlaying = false;
            StopPositionTimer();
            SyncPositionFromPlayer();
            SetState(AudioPlaybackState.Paused, _currentAudioUrl);
            RaisePositionChanged();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task ResumeAsync()
    {
        // Tiếp tục phát từ vị trí đã pause nếu player còn hợp lệ.
        // Dùng trong flow play/pause và resume theo checkpoint.
        await _operationLock.WaitAsync();
        try
        {
            if (_isPlaying || _player == null || string.IsNullOrWhiteSpace(_currentAudioUrl))
            {
                return;
            }

            _player.Play();
            _isPlaying = true;
            SetState(AudioPlaybackState.Playing, _currentAudioUrl);
            StartPositionTimer();
            RaisePositionChanged();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task StopAsync()
    {
        // Dừng hẳn audio, hủy load pending và reset state hiện tại.
        // Dùng khi đổi bài, đổi POI hoặc kết thúc luồng auto/manual.
        CancelPendingLoad();
        await _operationLock.WaitAsync();
        try
        {
            _player?.Stop();
            StopPositionTimer();
            CleanupPlayer();

            _isPlaying = false;
            _currentPosition = TimeSpan.Zero;
            _duration = TimeSpan.Zero;

            var stoppedUrl = _currentAudioUrl;
            _currentAudioUrl = null;
            _currentLocationId = null;
            _currentAudioGuideId = null;
            SetState(AudioPlaybackState.Stopped, stoppedUrl);
            RaisePositionChanged();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task SeekAsync(TimeSpan position)
    {
        // Nhảy tới vị trí cần nghe (seek) và phát sự kiện cập nhật progress.
        // Thuộc flow tua tiến/lùi 10s và kéo slider.
        await _operationLock.WaitAsync();
        try
        {
            if (_player == null)
            {
                return;
            }

            var safeDuration = _duration > TimeSpan.Zero
                ? _duration
                : TimeSpan.FromSeconds(Math.Max(0, _player.Duration));

            _currentPosition = position;
            if (_currentPosition > safeDuration)
            {
                _currentPosition = safeDuration;
            }

            if (_currentPosition < TimeSpan.Zero)
            {
                _currentPosition = TimeSpan.Zero;
            }

            if (_player.CanSeek)
            {
                _player.Seek(_currentPosition.TotalSeconds);
            }

            _duration = safeDuration;
            RaisePositionChanged();
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public Task SetVolumeAsync(double volume)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);
        if (_player != null)
        {
            _player.Volume = _volume;
        }
        return Task.CompletedTask;
    }

    private void StartPositionTimer()
    {
        StopPositionTimer();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(150);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
    }

    private void StopPositionTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isPlaying || _player == null) return;

        SyncPositionFromPlayer();
        _duration = TimeSpan.FromSeconds(Math.Max(0, _player.Duration));

        if (_duration > TimeSpan.Zero && _currentPosition >= _duration)
        {
            _currentPosition = _duration;
            _isPlaying = false;
            StopPositionTimer();
            SetState(AudioPlaybackState.Stopped, _currentAudioUrl);
        }

        RaisePositionChanged();
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _player))
        {
            return;
        }

        _isPlaying = false;
        StopPositionTimer();
        SyncPositionFromPlayer();
        SetState(AudioPlaybackState.Stopped, _currentAudioUrl);
        RaisePositionChanged();
    }

    private async Task LoadPlayerAsync(string source, CancellationToken cancellationToken)
    {
        // Tạo player từ file local hoặc stream HTTP, hỗ trợ cả URL Cloudinary.
        // Là lõi load dữ liệu audio trước khi gọi Play().
        CleanupPlayer();

        if (File.Exists(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _player = _audioManager.CreatePlayer(source);
            return;
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var memoryStream = new MemoryStream();
            await networkStream.CopyToAsync(memoryStream, 81920, cancellationToken);
            memoryStream.Position = 0;

            _sourceStream = memoryStream;
            _player = _audioManager.CreatePlayer(_sourceStream);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _player = _audioManager.CreatePlayer(source);
    }

    private void SyncPositionFromPlayer()
    {
        if (_player == null)
        {
            return;
        }

        _currentPosition = TimeSpan.FromSeconds(Math.Max(0, _player.CurrentPosition));
    }

    private void CleanupPlayer()
    {
        if (_player != null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;

            try
            {
                _player.Stop();
            }
            catch
            {
                // Ignore stop errors during disposal cleanup.
            }

            if (_player is IDisposable disposablePlayer)
            {
                disposablePlayer.Dispose();
            }
        }

        _player = null;

        if (_sourceStream != null)
        {
            _sourceStream.Dispose();
            _sourceStream = null;
        }
    }

    private CancellationTokenSource ReplaceActiveLoadCts()
    {
        lock (_loadSync)
        {
            _activeLoadCts?.Cancel();
            _activeLoadCts?.Dispose();
            _activeLoadCts = new CancellationTokenSource();
            return _activeLoadCts;
        }
    }

    private void ClearActiveLoadCts(CancellationTokenSource expected)
    {
        lock (_loadSync)
        {
            if (!ReferenceEquals(_activeLoadCts, expected))
            {
                return;
            }

            _activeLoadCts = null;
        }
    }

    private void CancelPendingLoad()
    {
        lock (_loadSync)
        {
            _activeLoadCts?.Cancel();
        }
    }

    private void RaisePositionChanged()
    {
        PositionChanged?.Invoke(this, new AudioPositionChangedEventArgs
        {
            Position = _currentPosition,
            Duration = _duration
        });
    }

    private void SetState(AudioPlaybackState state, string? audioUrl)
    {
        _state = state;
        StateChanged?.Invoke(this, new AudioStateChangedEventArgs
        {
            State = state,
            AudioUrl = audioUrl
        });
    }

    public void Dispose()
    {
        CancelPendingLoad();
        StopPositionTimer();
        CleanupPlayer();
        _activeLoadCts?.Dispose();
        _activeLoadCts = null;
        _operationLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
