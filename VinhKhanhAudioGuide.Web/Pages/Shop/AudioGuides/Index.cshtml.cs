using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public List<AudioGuide> AudioGuides { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        IQueryable<Location> locationQuery = _db.Locations.AsNoTracking();

        if (!UserAccessService.IsAdmin(User))
        {
            var ownedLocationIds = UserAccessService.GetOwnedLocationIds(User);
            locationQuery = locationQuery.Where(location => ownedLocationIds.Contains(location.Id));
        }

        AccessibleLocations = await locationQuery.OrderBy(location => location.Name).ToListAsync();

        if (!AccessibleLocations.Any())
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(LocationId))
        {
            LocationId = AccessibleLocations[0].Id;
        }

        if (!UserAccessService.CanAccessLocation(User, LocationId))
        {
            return Forbid();
        }

        AudioGuides = await _db.AudioGuides
            .AsNoTracking()
            .Where(audioGuide => audioGuide.LocationId == LocationId)
            .OrderBy(audioGuide => audioGuide.Title)
            .ToListAsync();

        return Page();
    }
}
