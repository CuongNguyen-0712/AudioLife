namespace VinhKhanhAudioGuide.Mobile.Models;

public sealed record AppSessionSnapshot(
    string DeviceId,
    string SessionToken,
    string? RefreshToken,
    string? QrToken,
    string? UserAppId,
    string? PackageId,
    string? PaymentStatus,
    string? PaymentReference,
    string? LocationId,
    string? AudioGuideId,
    string? AudioUrl,
    DateTime ExpiresAtUtc,
    DateTime LastValidatedAtUtc)
{
    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;
}