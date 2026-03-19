using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
    public List<CategoryItem> CategoryBreakdown { get; set; } = new();
    public List<LocationItem> TopLocationsByAudio { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalAudios = await _db.AudioGuides.CountAsync();
        TotalLocations = await _db.Locations.CountAsync();
        TotalTours = await _db.Tours.CountAsync();
        AvgAudioPerLocation = TotalLocations == 0 ? 0 : Math.Round((double)TotalAudios / TotalLocations, 1);

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
}
