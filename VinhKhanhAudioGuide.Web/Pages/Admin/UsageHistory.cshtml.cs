using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class UsageHistoryModel : PageModel
{
    private readonly AppDbContext _db;

    public UsageHistoryModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "all";

    public List<LocationFilterItem> Locations { get; set; } = new();
    public List<HistoryRow> RecentItems { get; set; } = new();
    public List<LocationUsageItem> TopLocations { get; set; } = new();

    public int TotalRecords { get; set; }
    public int CompletedRecords { get; set; }
    public int IncompleteRecords { get; set; }
    public long TotalListenedSeconds { get; set; }
    public double AverageProgressPercent { get; set; }
    public DateTime? LastActivityUtc { get; set; }

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new LocationFilterItem
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToListAsync();

        var query = _db.ListeningHistories
            .AsNoTracking()
            .Include(item => item.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(LocationId))
        {
            query = query.Where(item => item.LocationId == LocationId);
        }

        if (string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => item.IsCompleted);
        }
        else if (string.Equals(Status, "incomplete", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => !item.IsCompleted);
        }

        TotalRecords = await query.CountAsync();
        CompletedRecords = await query.CountAsync(item => item.IsCompleted);
        IncompleteRecords = TotalRecords - CompletedRecords;
        TotalListenedSeconds = await query.SumAsync(item => (long?)item.ListenedSeconds) ?? 0;

        var averageProgress = await query.AverageAsync(item => (decimal?)item.Progress) ?? 0M;
        AverageProgressPercent = Math.Round((double)averageProgress * 100, 1);

        LastActivityUtc = await query.MaxAsync(item => (DateTime?)item.LastListenedAtUtc);

        TopLocations = await query
            .GroupBy(item => new
            {
                item.LocationId,
                LocationName = item.Location != null ? item.Location.Name : item.LocationId
            })
            .Select(group => new LocationUsageItem
            {
                LocationId = group.Key.LocationId,
                LocationName = group.Key.LocationName,
                RecordCount = group.Count(),
                CompletedCount = group.Count(item => item.IsCompleted),
                ListenSeconds = group.Sum(item => item.ListenedSeconds),
                AvgProgressPercent = Math.Round((double)group.Average(item => item.Progress) * 100, 1)
            })
            .OrderByDescending(item => item.ListenSeconds)
            .ThenByDescending(item => item.RecordCount)
            .Take(10)
            .ToListAsync();

        RecentItems = await query
            .OrderByDescending(item => item.LastListenedAtUtc)
            .Take(100)
            .Select(item => new HistoryRow
            {
                Id = item.Id,
                AudioGuideId = item.AudioGuideId,
                AudioTitle = item.AudioTitle,
                LocationId = item.LocationId,
                LocationName = item.Location != null ? item.Location.Name : item.LocationName,
                IsCompleted = item.IsCompleted,
                ListenedSeconds = item.ListenedSeconds,
                ProgressPercent = Math.Round((double)item.Progress * 100, 1),
                LastListenedAtUtc = item.LastListenedAtUtc
            })
            .ToListAsync();
    }

    public sealed class LocationFilterItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public sealed class HistoryRow
    {
        public string Id { get; set; } = string.Empty;
        public string AudioGuideId { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int ListenedSeconds { get; set; }
        public double ProgressPercent { get; set; }
        public DateTime LastListenedAtUtc { get; set; }
    }

    public sealed class LocationUsageItem
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public int CompletedCount { get; set; }
        public int ListenSeconds { get; set; }
        public double AvgProgressPercent { get; set; }
    }
}
