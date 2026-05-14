using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class ReportsModel : PageModel
{
    private readonly AppDbContext _db;

    public ReportsModel(AppDbContext db)
    {
        _db = db;
    }

    public int TotalAudios { get; set; }
    public int TotalLocations { get; set; }
    public int TotalTours { get; set; }
    public double AvgAudioPerLocation { get; set; }
    
    public int TotalListens { get; set; }
    public double AvgListeningSeconds { get; set; }
    
    public List<CategoryItem> CategoryBreakdown { get; set; } = new();
    public List<LocationItem> TopLocationsByAudio { get; set; } = new();
    public List<LocationListenItem> TopLocationsByListens { get; set; } = new();
    public List<DailyListenItem> Last30DaysStats { get; set; } = new();
    public List<HourlyListenItem> HourlyDistribution { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalAudios = await _db.AudioGuides.CountAsync();
        TotalLocations = await _db.Locations.CountAsync();
        TotalTours = await _db.Tours.CountAsync();
        AvgAudioPerLocation = TotalLocations == 0 ? 0 : Math.Round((double)TotalAudios / TotalLocations, 1);

        TotalListens = await _db.ListeningHistories.CountAsync();
        AvgListeningSeconds = TotalListens == 0 ? 0 : await _db.ListeningHistories.AverageAsync(x => x.ListenedSeconds);

        CategoryBreakdown = await _db.Categories
            .AsNoTracking()
            .Select(category => new CategoryItem
            {
                Name = category.Name,
                AudioCount = category.Locations.SelectMany(location => location.AudioGuides).Count()
            })
            .OrderByDescending(item => item.AudioCount)
            .ToListAsync();

        TopLocationsByAudio = await _db.Locations
            .AsNoTracking()
            .Select(location => new LocationItem
            {
                Name = location.Name,
                AudioCount = location.AudioGuides.Count,
                Category = location.Category != null ? location.Category.Name : "Khác"
            })
            .OrderByDescending(item => item.AudioCount)
            .ThenBy(item => item.Name)
            .Take(8)
            .ToListAsync();

        TopLocationsByListens = await _db.ListeningHistories
            .AsNoTracking()
            .GroupBy(x => new { x.LocationId, x.LocationName })
            .Select(g => new LocationListenItem
            {
                LocationId = g.Key.LocationId,
                LocationName = g.Key.LocationName,
                ListenCount = g.Count(),
                AvgSeconds = g.Average(x => x.ListenedSeconds)
            })
            .OrderByDescending(item => item.ListenCount)
            .Take(10)
            .ToListAsync();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        Last30DaysStats = await _db.ListeningHistories
            .AsNoTracking()
            .Where(x => x.LastListenedAtUtc >= thirtyDaysAgo)
            .GroupBy(x => x.LastListenedAtUtc.Date)
            .Select(g => new DailyListenItem
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        HourlyDistribution = await _db.ListeningHistories
            .AsNoTracking()
            .GroupBy(x => x.LastListenedAtUtc.Hour)
            .Select(g => new HourlyListenItem
            {
                Hour = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Hour)
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var topListens = await _db.ListeningHistories
            .AsNoTracking()
            .GroupBy(x => new { x.LocationId, x.LocationName })
            .Select(g => new
            {
                LocationId = g.Key.LocationId,
                LocationName = g.Key.LocationName,
                ListenCount = g.Count(),
                AvgSeconds = g.Average(x => x.ListenedSeconds)
            })
            .OrderByDescending(item => item.ListenCount)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Location ID,Location Name,Total Listens,Avg Listening Time (seconds)");
        
        foreach (var item in topListens)
        {
            sb.AppendLine($"{EscapeCsv(item.LocationId)},{EscapeCsv(item.LocationName)},{item.ListenCount},{Math.Round(item.AvgSeconds, 1)}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        // Add BOM for Excel UTF-8 compatibility
        var bom = Encoding.UTF8.GetPreamble();
        var result = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, result, bom.Length, bytes.Length);

        return File(result, "text/csv", $"ListeningStats_{DateTime.Now:yyyyMMdd}.csv");
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    public class CategoryItem
    {
        public string Name { get; set; } = string.Empty;
        public int AudioCount { get; set; }
    }

    public class LocationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int AudioCount { get; set; }
    }

    public class LocationListenItem
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
        public double AvgSeconds { get; set; }
    }

    public class DailyListenItem
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class HourlyListenItem
    {
        public int Hour { get; set; }
        public int Count { get; set; }
    }
}
