namespace VinhKhanhAudioGuide.Web.Models;

public class MobileCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LocationCount { get; set; }
}

public class MobileLocationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Priority { get; set; }
    public double DetectionRadiusMeters { get; set; }
    public int Duration { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public string ResolvedLanguage { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<MobileAudioGuideDto> AudioGuides { get; set; } = new();
}

public class MobileTourDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public List<string> LocationIds { get; set; } = new();
    public decimal Price { get; set; }
    public bool IsFeatured { get; set; }
}

public class MobileAudioGuideDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string? CloudinaryAudioUrl { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
    public string ResolvedLanguage { get; set; } = string.Empty;
    public List<MobileAudioScriptSegmentDto> ScriptSegments { get; set; } = new();
}

public class MobileAudioScriptSegmentDto
{
    public int Id { get; set; }
    public string AudioGuideId { get; set; } = string.Empty;
    public int StartTimeSeconds { get; set; }
    public int EndTimeSeconds { get; set; }
    public string ScriptText { get; set; } = string.Empty;
}

public class MobileListeningHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string AudioGuideId { get; set; } = string.Empty;
    public string AudioTitle { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string LocationImageUrl { get; set; } = string.Empty;
    public int AudioDuration { get; set; }
    public double Progress { get; set; }
    public DateTime ListenedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ListenedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastListenedAt { get; set; }
}

public class MobileHeartbeatRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? ActivityName { get; set; }
    public string? ActivityContext { get; set; }
    public string? ScreenName { get; set; }
    public string? Route { get; set; }
    public bool IsForeground { get; set; } = true;
}

public class MobileHeartbeatResponse
{
    public bool Success { get; set; }
    public bool SessionValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserAppId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? CurrentActivity { get; set; }
    public DateTime CurrentActivityAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime LastValidatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class MobilePaymentPackageDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class MobilePaymentCompletionRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? QrToken { get; set; }
    public string? UserAppId { get; set; }
    public string? LocationId { get; set; }
    public string? AudioGuideId { get; set; }
    public string? AudioUrl { get; set; }
    public string PackageId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
}

public class MobilePaymentCompletionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserAppId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string PackageId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastValidatedAtUtc { get; set; }
}

public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserAppId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? PackageId { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastValidatedAtUtc { get; set; }
}

public class DeviceSessionCheckResult
{
    public bool HasSession { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserAppId { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? PackageId { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastValidatedAtUtc { get; set; }
}

public class MobileAddListeningHistoryRequest
{
    public string AudioGuideId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public double Progress { get; set; }
    public int ListenedSeconds { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Request payload for refreshing an expired JWT using a stored RefreshToken.
/// </summary>
public class SessionRefreshRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string? SessionToken { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class MobileLocationReviewDto
{
    public Guid Id { get; set; }
    public string LocationId { get; set; } = string.Empty;

    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileSubmitReviewRequest
{
    public string LocationId { get; set; } = string.Empty;

    public int Rating { get; set; }
    public string? Comment { get; set; }
}
public class MobileDeviceRegistrationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty; // Mapping to FCMToken
    public string? Platform { get; set; } // Mapping to DeviceType/Platform
}
