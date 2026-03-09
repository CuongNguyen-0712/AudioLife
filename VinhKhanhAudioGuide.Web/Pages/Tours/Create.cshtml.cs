using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Tours;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Tour Tour { get; set; } = new();

    [BindProperty]
    public List<string> SelectedLocationIds { get; set; } = new();

    public List<Location> AllLocations { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllLocations = await _db.Locations.OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("SelectedLocationIds");
        if (!ModelState.IsValid)
        {
            AllLocations = await _db.Locations.OrderBy(l => l.Name).ToListAsync();
            return Page();
        }

        _db.Tours.Add(Tour);
        await _db.SaveChangesAsync();

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

        TempData["Success"] = $"Đã thêm tour \"{Tour.Name}\"";
        return RedirectToPage("Index");
    }
}
