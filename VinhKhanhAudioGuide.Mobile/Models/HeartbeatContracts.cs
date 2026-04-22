namespace VinhKhanhAudioGuide.Mobile.Models;

public sealed record HeartbeatRequest(
    string DeviceId,
    string SessionToken,
    string? ActivityName,
    string? ActivityContext,
    string? ScreenName,
    string? Route,
    bool IsForeground = true);

public sealed record HeartbeatResponse(
    bool Success,
    bool SessionValid,
    string Message,
    string UserAppId,
    string SessionToken,
    string? CurrentActivity,
    DateTime CurrentActivityAtUtc,
    DateTime LastSeenAtUtc,
    DateTime LastValidatedAtUtc,
    DateTime ExpiresAtUtc);