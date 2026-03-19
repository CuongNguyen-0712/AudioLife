using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Location> Locations { get; set; } = new();
    public Location? SelectedLocation { get; set; }
    public int AudioGuideCount { get; set; }
    public int AudioDurationMinutes { get; set; }
    public int TourCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<AudioGuide> RecentAudioGuides { get; set; } = new();
    public List<Tour> RelatedTours { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SelectedLocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        IQueryable<Location> query = _db.Locations.AsNoTracking();

        if (!UserAccessService.IsAdmin(User))
        {
            var ownedLocationIds = UserAccessService.GetOwnedLocationIds(User);
            query = query.Where(location => ownedLocationIds.Contains(location.Id));
        }

        Locations = await query.OrderBy(location => location.Name).ToListAsync();

        if (!Locations.Any())
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(SelectedLocationId))
        {
            SelectedLocationId = Locations[0].Id;
        }

        if (!UserAccessService.CanAccessLocation(User, SelectedLocationId))
        {
            return Forbid();
        }

        SelectedLocation = await _db.Locations
            .AsNoTracking()
            .Include(location => location.Category)
            .FirstOrDefaultAsync(l => l.Id == SelectedLocationId);

        if (SelectedLocation is null)
        {
            return Page();
        }

        AudioGuideCount = await _db.AudioGuides
            .AsNoTracking()
            .CountAsync(ag => ag.LocationId == SelectedLocation.Id);

        AudioDurationMinutes = await _db.AudioGuides
            .AsNoTracking()
            .Where(ag => ag.LocationId == SelectedLocation.Id)
            .Select(ag => ag.Duration)
            .SumAsync();

        RecentAudioGuides = await _db.AudioGuides
            .AsNoTracking()
            .Where(ag => ag.LocationId == SelectedLocation.Id)
            .OrderByDescending(ag => ag.Id)
            .Take(5)
            .ToListAsync();

        var relatedToursRaw = await _db.TourLocations
            .AsNoTracking()
            .Where(tl => tl.LocationId == SelectedLocation.Id)
            .OrderBy(tl => tl.SortOrder)
            .Select(tl => tl.Tour)
            .ToListAsync();

        RelatedTours = relatedToursRaw
            .Where(tour => tour != null)
            .Select(tour => tour!)
            .GroupBy(tour => tour.Id)
            .Select(group => group.First())
            .ToList();

        TourCount = RelatedTours.Count;
        CategoryName = SelectedLocation.Category?.Name ?? "Chưa phân loại";

        return Page();
    }
}
