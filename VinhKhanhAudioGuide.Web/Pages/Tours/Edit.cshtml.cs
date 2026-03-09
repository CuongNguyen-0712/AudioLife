using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Tours;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Tour Tour { get; set; } = new();

    [BindProperty]
    public List<string> SelectedLocationIds { get; set; } = new();

    public List<Location> AllLocations { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var tour = await _db.Tours
            .Include(t => t.TourLocations)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tour == null) return NotFound();

        Tour = tour;
        SelectedLocationIds = tour.TourLocations.OrderBy(tl => tl.SortOrder).Select(tl => tl.LocationId).ToList();
        AllLocations = await _db.Locations.OrderBy(l => l.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("SelectedLocationIds");
        if (!ModelState.IsValid)
        {
            AllLocations = await _db.Locations.OrderBy(l => l.Name).ToListAsync();
            return Page();
        }

        _db.Tours.Update(Tour);

        var existing = await _db.TourLocations.Where(tl => tl.TourId == Tour.Id).ToListAsync();
        _db.TourLocations.RemoveRange(existing);

        for (int i = 0; i < SelectedLocationIds.Count; i++)
        {
            _db.TourLocations.Add(new TourLocation
            {
                TourId = Tour.Id,
                LocationId = SelectedLocationIds[i],
                SortOrder = i
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật tour \"{Tour.Name}\"";
        return RedirectToPage("Index");
    }
}
