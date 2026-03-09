using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Tours;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Tour Tour { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var tour = await _db.Tours.FindAsync(id);
        if (tour == null) return NotFound();
        Tour = tour;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tour = await _db.Tours
            .Include(t => t.TourLocations)
            .FirstOrDefaultAsync(t => t.Id == Tour.Id);

        if (tour == null) return NotFound();

        _db.TourLocations.RemoveRange(tour.TourLocations);
        _db.Tours.Remove(tour);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa tour \"{tour.Name}\"";
        return RedirectToPage("Index");
    }
}
