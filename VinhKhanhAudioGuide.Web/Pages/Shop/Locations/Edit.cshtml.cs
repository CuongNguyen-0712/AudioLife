using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.Locations;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Location Location { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!UserAccessService.CanAccessLocation(User, id))
        {
            return Forbid();
        }

        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (location is null)
        {
            return NotFound();
        }

        Location = location;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Location.Category");
        ModelState.Remove("Location.AudioGuides");
        ModelState.Remove("Location.TourLocations");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!UserAccessService.CanAccessLocation(User, Location.Id))
        {
            return Forbid();
        }

        var entity = await _db.Locations.FirstOrDefaultAsync(item => item.Id == Location.Id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = Location.Name;
        entity.Description = Location.Description;
        entity.Address = Location.Address;
        entity.Latitude = Location.Latitude;
        entity.Longitude = Location.Longitude;
        entity.Duration = Location.Duration;
        entity.ImageUrl = Location.ImageUrl;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật địa điểm.";
        return RedirectToPage("/Shop/Index", new { locationId = entity.Id });
    }
}
