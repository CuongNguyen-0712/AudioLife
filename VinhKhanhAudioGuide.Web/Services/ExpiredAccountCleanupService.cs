using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Services;

public class ExpiredAccountCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredAccountCleanupService> _logger;

    public ExpiredAccountCleanupService(IServiceProvider serviceProvider, ILogger<ExpiredAccountCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Background loop chạy định kỳ 6 giờ để quét account hết hiệu lực.
        // Thuộc flow maintenance và dọn dẹp dữ liệu hệ thống.
        _logger.LogInformation("ExpiredAccountCleanupService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing ExpiredAccountCleanupService.");
            }

            // Chạy dọn dẹp mỗi 6 tiếng
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task CleanupExpiredAccountsAsync(CancellationToken cancellationToken)
    {
        // Soft-delete user không còn subscription active để giữ lịch sử nhưng khóa sử dụng.
        // Thuộc flow quản trị vòng đời tài khoản.
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var nowUtc = DateTime.UtcNow;

        // Tìm tất cả user chưa bị xóa (IsDeleted = false)
        var usersToCheck = await db.AppUsers
            .Include(u => u.Subscriptions)
            .Where(u => !u.IsDeleted)
            .ToListAsync(cancellationToken);

        int softDeletedCount = 0;

        foreach (var user in usersToCheck)
        {
            // Kiểm tra xem user có bất kỳ subscription nào đang active hay không
            // Gói active là gói có Status == "Active" và ExpiresAtUtc > nowUtc
            var hasActiveSubscription = user.Subscriptions
                .Any(s => s.Status == "Active" && (s.ExpiresAtUtc == null || s.ExpiresAtUtc > nowUtc));

            if (!hasActiveSubscription)
            {
                // Nếu user không có gói nào active -> soft delete
                // Ngoài ra có thể kiểm tra xem session cuối cùng hết hạn bao lâu, 
                // nhưng nếu dựa trên gói thì chỉ cần không có gói active là đánh dấu soft delete
                user.IsDeleted = true;
                user.Status = "Expired";
                softDeletedCount++;
            }
        }

        if (softDeletedCount > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Soft deleted {Count} expired accounts.", softDeletedCount);
        }
    }
}
