namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Audio playback service with event-based state notifications.
/// Includes built-in cooldown to prevent rapid successive plays.
/// </summary>
public interface IAudioService
{
    TimeSpan CurrentPosition { get; }
    TimeSpan Duration { get; }
    bool IsPlaying { get; }
    double Volume { get; }
    string? CurrentAudioUrl { get; }
    string? CurrentLocationId { get; }
    string? CurrentAudioGuideId { get; }
    bool IsDirectTap { get; }
    DateTime LastPlayAttemptUtc { get; }
    TimeSpan PlayCooldown { get; }

    event EventHandler<AudioStateChangedEventArgs>? StateChanged;
    event EventHandler<AudioPositionChangedEventArgs>? PositionChanged;

    Task PlayAsync(string audioUrl);
    Task PlayAsync(string audioUrl, string locationId, string audioGuideId, bool isDirectTap = false);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    Task SeekAsync(TimeSpan position);
    Task SetVolumeAsync(double volume);
}

public class AudioStateChangedEventArgs : EventArgs
{
    public AudioPlaybackState State { get; init; }
    public string? AudioUrl { get; init; }
}

public class AudioPositionChangedEventArgs : EventArgs
{
    public TimeSpan Position { get; init; }
    public TimeSpan Duration { get; init; }
}

public enum AudioPlaybackState
{
    None,
    Loading,
    Playing,
    Paused,
    Stopped,
    Error
}
