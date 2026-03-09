namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Audio playback service using a timer-based simulation.
/// In production, integrate with CommunityToolkit.Maui.MediaElement or platform audio APIs.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isPlaying;
    private double _volume = 1.0;
    private string? _currentAudioUrl;
    private IDispatcherTimer? _timer;
    private AudioPlaybackState _state = AudioPlaybackState.None;

    public TimeSpan CurrentPosition => _currentPosition;
    public TimeSpan Duration => _duration;
    public bool IsPlaying => _isPlaying;
    public double Volume => _volume;
    public string? CurrentAudioUrl => _currentAudioUrl;

    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;
    public event EventHandler<AudioPositionChangedEventArgs>? PositionChanged;

    public async Task PlayAsync(string audioUrl)
    {
        // Stop any current playback
        if (_isPlaying)
        {
            await StopAsync();
        }

        _currentAudioUrl = audioUrl;
        SetState(AudioPlaybackState.Loading);

        // Simulate loading delay (replace with real audio loading)
        await Task.Delay(300);

        // Simulate duration based on the audio URL or use a default
        _duration = TimeSpan.FromMinutes(GetSimulatedDuration(audioUrl));
        _currentPosition = TimeSpan.Zero;
        _isPlaying = true;

        SetState(AudioPlaybackState.Playing);
        StartPositionTimer();
    }

    public Task PauseAsync()
    {
        if (!_isPlaying) return Task.CompletedTask;

        _isPlaying = false;
        StopPositionTimer();
        SetState(AudioPlaybackState.Paused);
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_isPlaying || _currentAudioUrl == null) return Task.CompletedTask;

        _isPlaying = true;
        SetState(AudioPlaybackState.Playing);
        StartPositionTimer();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isPlaying = false;
        _currentPosition = TimeSpan.Zero;
        StopPositionTimer();
        SetState(AudioPlaybackState.Stopped);
        _currentAudioUrl = null;
        return Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position)
    {
        _currentPosition = position;
        if (_currentPosition > _duration)
            _currentPosition = _duration;
        if (_currentPosition < TimeSpan.Zero)
            _currentPosition = TimeSpan.Zero;

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
        return Task.CompletedTask;
    }

    private void StartPositionTimer()
    {
        StopPositionTimer();
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
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
        if (!_isPlaying) return;

        _currentPosition = _currentPosition.Add(TimeSpan.FromSeconds(1));

        if (_currentPosition >= _duration)
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

    private void SetState(AudioPlaybackState state)
    {
        _state = state;
        StateChanged?.Invoke(this, new AudioStateChangedEventArgs
        {
            State = state,
            AudioUrl = _currentAudioUrl
        });
    }

    private static double GetSimulatedDuration(string audioUrl)
    {
        // Simulate different durations based on audio URL patterns
        if (audioUrl.Contains("history")) return 5;
        if (audioUrl.Contains("architecture")) return 4;
        if (audioUrl.Contains("spiritual")) return 6;
        if (audioUrl.Contains("overview")) return 10;
        if (audioUrl.Contains("guide")) return 8;
        return 5; // default 5 minutes
    }

    public void Dispose()
    {
        StopPositionTimer();
        GC.SuppressFinalize(this);
    }
}
