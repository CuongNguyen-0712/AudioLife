namespace VinhKhanhAudioGuide.Mobile.Services;

using Plugin.Maui.Audio;

/// <summary>
/// Audio playback service backed by Plugin.Maui.Audio.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private static readonly HttpClient HttpClient = new();

    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    private Stream? _sourceStream;

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isPlaying;
    private double _volume = 1.0;
    private AudioPlaybackState _state = AudioPlaybackState.None;
    private string? _currentAudioUrl;
    private IDispatcherTimer? _timer;

    public TimeSpan CurrentPosition => _currentPosition;
    public TimeSpan Duration => _duration;
    public bool IsPlaying => _isPlaying;
    public double Volume => _volume;
    public string? CurrentAudioUrl => _currentAudioUrl;

    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioPositionChangedEventArgs>? PositionChanged;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task PlayAsync(string audioUrl)
    {
        if (_isPlaying)
        {
            await StopAsync();
        }

        _currentAudioUrl = audioUrl;
        SetState(AudioPlaybackState.Loading);

        try
        {
            await LoadPlayerAsync(audioUrl);
            if (_player == null)
            {
                throw new InvalidOperationException("Không thể khởi tạo audio player.");
            }

            _player.Volume = _volume;
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();

            _duration = TimeSpan.FromSeconds(Math.Max(0, _player.Duration));
            _currentPosition = TimeSpan.Zero;
            _isPlaying = true;

            SetState(AudioPlaybackState.Playing);
            StartPositionTimer();
        }
        catch
        {
            CleanupPlayer();
            SetState(AudioPlaybackState.Error);
            throw;
        }
    }

    public Task PauseAsync()
    {
        if (!_isPlaying || _player == null) return Task.CompletedTask;

        _player.Pause();
        _isPlaying = false;
        StopPositionTimer();
        SyncPositionFromPlayer();
        SetState(AudioPlaybackState.Paused);
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_isPlaying || _player == null || _currentAudioUrl == null) return Task.CompletedTask;

        _player.Play();
        _isPlaying = true;
        SetState(AudioPlaybackState.Playing);
        StartPositionTimer();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _player?.Stop();
        StopPositionTimer();
        CleanupPlayer();

        _isPlaying = false;
        _currentPosition = TimeSpan.Zero;
        _duration = TimeSpan.Zero;
        SetState(AudioPlaybackState.Stopped);
        _currentAudioUrl = null;
        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position)
    {
        if (_player == null)
        {
            return Task.CompletedTask;
        }

        _currentPosition = position;
        if (_currentPosition > _duration)
            _currentPosition = _duration;
        if (_currentPosition < TimeSpan.Zero)
            _currentPosition = TimeSpan.Zero;

        if (_player.CanSeek)
        {
            _player.Seek(_currentPosition.TotalSeconds);
        }

        PositionChanged?.Invoke(this, new AudioPositionChangedEventArgs
        {
            Position = _currentPosition,
            Duration = _duration
        });
        return Task.CompletedTask;
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
            _timer.Interval = TimeSpan.FromMilliseconds(300);
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
            SetState(AudioPlaybackState.Stopped);
        }

        PositionChanged?.Invoke(this, new AudioPositionChangedEventArgs
        {
            Position = _currentPosition,
            Duration = _duration
        });
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _isPlaying = false;
        StopPositionTimer();
        SyncPositionFromPlayer();
        SetState(AudioPlaybackState.Stopped);
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

    private void SetState(AudioPlaybackState state)
    {
        _state = state;
        StateChanged?.Invoke(this, new AudioStateChangedEventArgs
        {
            State = state,
            AudioUrl = _currentAudioUrl
        });
    }

    public void Dispose()
    {
        StopPositionTimer();
        CleanupPlayer();
        GC.SuppressFinalize(this);
    }
}
