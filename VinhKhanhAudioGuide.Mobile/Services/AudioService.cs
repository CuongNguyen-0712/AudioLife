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
    private IAudioPlayer? _player;
    private Stream? _sourceStream;

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isPlaying;
    private double _volume = 1.0;
    private AudioPlaybackState _state = AudioPlaybackState.None;
    private string? _currentAudioUrl;
    private IDispatcherTimer? _timer;
    private long _playRequestVersion;
    private DateTime _lastPlayAttemptUtc = DateTime.MinValue;
    private readonly TimeSpan _playCooldown = TimeSpan.FromSeconds(DefaultPlayCooldownSeconds);

    public TimeSpan CurrentPosition => _currentPosition;
    public TimeSpan Duration => _duration;
    public bool IsPlaying => _isPlaying;
    public double Volume => _volume;
    public string? CurrentAudioUrl => _currentAudioUrl;
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
            await LoadPlayerAsync(audioUrl);
            if (_player == null)
            {
                throw new InvalidOperationException("Không thể khởi tạo audio player.");
            }

            // Ignore stale play requests that completed after a newer request.
            if (requestVersion != _playRequestVersion)
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
        catch
        {
            CleanupPlayer();
            _isPlaying = false;
            _currentPosition = TimeSpan.Zero;
            _duration = TimeSpan.Zero;
            SetState(AudioPlaybackState.Error, _currentAudioUrl);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task PauseAsync()
    {
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

    private async Task LoadPlayerAsync(string source)
    {
        CleanupPlayer();

        if (File.Exists(source))
        {
            _player = _audioManager.CreatePlayer(source);
            return;
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            var bytes = await HttpClient.GetByteArrayAsync(uri);
            _sourceStream = new MemoryStream(bytes);
            _player = _audioManager.CreatePlayer(_sourceStream);
            return;
        }

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
        StopPositionTimer();
        CleanupPlayer();
        GC.SuppressFinalize(this);
    }
}
