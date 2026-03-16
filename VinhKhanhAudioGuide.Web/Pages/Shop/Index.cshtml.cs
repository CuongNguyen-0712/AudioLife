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
            .FirstOrDefaultAsync(l => l.Id == SelectedLocationId);

        if (SelectedLocation is null)
        {
            return Page();
        }

        AudioGuideCount = await _db.AudioGuides
            .AsNoTracking()
            .CountAsync(ag => ag.LocationId == SelectedLocation.Id);

        return Page();
    }
}
