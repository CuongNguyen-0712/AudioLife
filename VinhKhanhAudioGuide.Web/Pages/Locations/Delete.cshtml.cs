using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Locations;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Location Location { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var loc = await _db.Locations.FindAsync(id);
        if (loc == null) return NotFound();
        Location = loc;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var loc = await _db.Locations
            .Include(l => l.AudioGuides)
            .Include(l => l.TourLocations)
            .FirstOrDefaultAsync(l => l.Id == Location.Id);

        if (loc == null) return NotFound();

        _db.AudioGuides.RemoveRange(loc.AudioGuides);
        _db.TourLocations.RemoveRange(loc.TourLocations);
        _db.Locations.Remove(loc);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa địa điểm \"{loc.Name}\"";
        return RedirectToPage("Index");
    }
}
