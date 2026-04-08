using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class AnalyticsModel : PageModel
{
    private readonly AppDbContext _db;

    public AnalyticsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public string SelectedLocationName { get; set; } = string.Empty;
    public int TotalAudios { get; set; }
    public int TotalAudioMinutes { get; set; }
    public List<AudioRankItem> TopAudios { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);
        IQueryable<Location> query = _db.Locations
            .AsNoTracking()
            .Where(location => ownedLocationIds.Contains(location.Id));

        AccessibleLocations = await query.OrderBy(location => location.Name).ToListAsync();
        if (!AccessibleLocations.Any()) return Page();

        if (string.IsNullOrWhiteSpace(LocationId))
        {
            LocationId = AccessibleLocations[0].Id;
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, LocationId)) return Forbid();

        var selectedLocation = AccessibleLocations.FirstOrDefault(location => location.Id == LocationId);
        SelectedLocationName = selectedLocation?.Name ?? string.Empty;

        var audios = await _db.AudioGuides
            .AsNoTracking()
            .Where(audio => audio.LocationId == LocationId)
            .OrderBy(audio => audio.Title)
            .ToListAsync();

        TotalAudios = audios.Count;
        TotalAudioMinutes = audios.Sum(audio => audio.Duration);

        TopAudios = audios
            .Select((audio, index) => new AudioRankItem
            {
                Title = audio.Title,
                Duration = audio.Duration,
                EstimatedListens = 120 + (index * 37)
            })
            .OrderByDescending(item => item.EstimatedListens)
            .Take(5)
            .ToList();

        return Page();
    }

    public class AudioRankItem
    {
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int EstimatedListens { get; set; }
    }
}
