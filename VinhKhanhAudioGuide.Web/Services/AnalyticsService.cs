using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IAnalyticsService
{
    Task<ListeningAnalyticsDto> GetListeningAnalyticsAsync(CancellationToken ct = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ListeningAnalyticsDto> GetListeningAnalyticsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalListeningSeconds = await _db.ListeningHistories
            .SumAsync(h => h.ListenedSeconds, ct);

        var totalSessions = await _db.ListeningHistories.CountAsync(ct);
        
        var topLocations = await _db.ListeningHistories
            .GroupBy(h => new { h.LocationId, h.LocationName })
            .Select(g => new TopLocationAnalytics
            {
                LocationId = g.Key.LocationId,
                LocationName = g.Key.LocationName,
                ListenCount = g.Count(),
                TotalSeconds = g.Sum(h => h.ListenedSeconds)
            })
            .OrderByDescending(x => x.ListenCount)
            .Take(5)
            .ToListAsync(ct);

        var completionRate = 0.0;
        if (await _db.ListeningHistories.AnyAsync(ct))
        {
            completionRate = await _db.ListeningHistories
                .AverageAsync(h => (double)h.Progress, ct);
        }

        var recentActivity = await _db.ListeningHistories
            .Where(h => h.LastListenedAtUtc >= thirtyDaysAgo)
            .GroupBy(h => h.LastListenedAtUtc.Date)
            .Select(g => new DailyActivity
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return new ListeningAnalyticsDto
        {
            TotalListeningSeconds = totalListeningSeconds,
            TotalSessions = totalSessions,
            AverageCompletionRate = (decimal)completionRate,
            TopLocations = topLocations,
            RecentDailyActivity = recentActivity
        };
    }
}

public class ListeningAnalyticsDto
{
    public int TotalListeningSeconds { get; set; }
    public int TotalSessions { get; set; }
    public decimal AverageCompletionRate { get; set; }
    public List<TopLocationAnalytics> TopLocations { get; set; } = new();
    public List<DailyActivity> RecentDailyActivity { get; set; } = new();
}

public class TopLocationAnalytics
{
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int ListenCount { get; set; }
    public int TotalSeconds { get; set; }
}

public class DailyActivity
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
