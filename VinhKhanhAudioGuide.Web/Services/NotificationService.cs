using Microsoft.EntityFrameworkCore;
namespace VinhKhanhAudioGuide.Web.Services;

public interface INotificationService
{
    Task<bool> SendPushNotificationAsync(Guid userId, string title, string body, object? data = null);
    Task<bool> RegisterDeviceTokenAsync(Guid userId, string deviceId, string fcmToken, string? platform = null);
    Task<bool> UnregisterDeviceTokenAsync(string deviceId, string fcmToken);
}

public class NotificationService : INotificationService
{
    private readonly Data.AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(Data.AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> RegisterDeviceTokenAsync(Guid userId, string deviceId, string fcmToken, string? platform = null)
    {
        try
        {
            var existing = await _db.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.DeviceId == deviceId && t.UserId == userId);

            if (existing != null)
            {
                existing.FCMToken = fcmToken;
                existing.Platform = platform ?? existing.Platform;
                existing.LastSeenAtUtc = DateTime.UtcNow;
                existing.IsActive = true;
            }
            else
            {
                var newToken = new Models.UserDeviceToken
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    FCMToken = fcmToken,
                    Platform = platform,
                    RegisteredAtUtc = DateTime.UtcNow,
                    LastSeenAtUtc = DateTime.UtcNow,
                    IsActive = true
                };
                _db.UserDeviceTokens.Add(newToken);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceTokenAsync(string deviceId, string fcmToken)
    {
        try
        {
            var tokens = await _db.UserDeviceTokens
                .Where(t => t.DeviceId == deviceId && t.FCMToken == fcmToken)
                .ToListAsync();

            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    token.IsActive = false;
                }
                await _db.SaveChangesAsync();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> SendPushNotificationAsync(Guid userId, string title, string body, object? data = null)
    {
        // Placeholder for FCM integration
        _logger.LogInformation("Sending push notification to UserId: {UserId}. Title: {Title}", userId, title);
        
        // In real implementation, we would use FirebaseAdmin SDK here
        // var tokens = await _db.UserDeviceTokens.Where(t => t.UserId == userId && t.IsActive).Select(t => t.FCMToken).ToListAsync();
        
        return await Task.FromResult(true);
    }
}
