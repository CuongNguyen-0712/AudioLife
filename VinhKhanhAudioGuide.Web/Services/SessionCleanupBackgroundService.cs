using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Services;

public class SessionCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupBackgroundService> _logger;

    public SessionCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<SessionCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired sessions.");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("Session Cleanup Background Service is stopping.");
    }

    private async Task DoCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nowUtc = DateTime.UtcNow;

        _logger.LogInformation("Starting periodic cleanup of expired sessions and subscriptions at {Time}", nowUtc);

        // 1. Cleanup expired sessions
        var sessionsToCleanup = await db.UserAppSessions
            .Where(s => (s.ExpiresAtUtc < nowUtc || s.RevokedAtUtc != null) && s.IsActive)
            .ToListAsync(ct);

        if (sessionsToCleanup.Any())
        {
            _logger.LogInformation("Deactivating {Count} expired/revoked sessions.", sessionsToCleanup.Count);
            foreach (var s in sessionsToCleanup)
            {
                s.IsActive = false;
            }
        }

        // 2. Optional: Cleanup or mark expired subscriptions if needed
        // (Subscription status management might be handled elsewhere, but we can sync here)
        var expiredSubscriptions = await db.UserSubscriptions
            .Where(s => s.Status == "Active" && s.ExpiresAtUtc < nowUtc)
            .ToListAsync(ct);

        if (expiredSubscriptions.Any())
        {
            _logger.LogInformation("Marking {Count} subscriptions as Expired.", expiredSubscriptions.Count);
            foreach (var sub in expiredSubscriptions)
            {
                sub.Status = "Expired";
            }
        }

        if (sessionsToCleanup.Any() || expiredSubscriptions.Any())
        {
            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Cleanup completed successfully.");
    }
}
