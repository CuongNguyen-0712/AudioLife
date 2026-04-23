using System;
using System.Threading.Tasks;

namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Service that manages automatic audio playback based on user location and proximity to points of interest.
/// Implements complex logic for auto-play, queueing, and manual interruptions.
/// </summary>
public interface IAutoPlaybackService
{
    /// <summary>
    /// Starts monitoring location updates for automatic playback.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops monitoring location updates.
    /// </summary>
    void Stop();

    /// <summary>
    /// Checks if auto-playback is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Notifies the service that a manual playback request was initiated by the user.
    /// This is used to handle TH4 (interrupting A with B).
    /// </summary>
    Task HandleManualPlaybackAsync(string locationId, string audioGuideId);
}
