namespace VinhKhanhAudioGuide.Mobile.Models;

public sealed record PaymentCompletionRequest(
    string DeviceId,
    string SessionToken,
    string? RefreshToken,
    string? QrToken,
    string? UserAppId,
    string? LocationId,
    string? AudioGuideId,
    string? AudioUrl,
    string PackageId,
    string PaymentStatus,
    string? PaymentReference);

public sealed record PaymentCompletionResult(
    bool Success,
    string Message,
    string UserAppId,
    string SessionToken,
    string? RefreshToken,
    string PackageId,
    string PaymentStatus,
    string? PaymentReference,
    DateTime ExpiresAtUtc,
    DateTime LastValidatedAtUtc);

public sealed record SessionValidationResult(
    bool IsValid,
    string Message,
    string UserAppId,
    string SessionToken,
    string? RefreshToken,
    string? PackageId,
    string? PaymentStatus,
    DateTime ExpiresAtUtc,
    DateTime LastValidatedAtUtc);

public sealed record DeviceSessionCheckResult(
    bool HasSession,
    string Message,
    string UserAppId,
    string SessionToken,
    string? RefreshToken,
    string? PackageId,
    string? PaymentStatus,
    DateTime ExpiresAtUtc,
    DateTime LastValidatedAtUtc);

