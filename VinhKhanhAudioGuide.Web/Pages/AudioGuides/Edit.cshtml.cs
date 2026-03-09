using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    public List<SelectListItem> LocationList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var ag = await _db.AudioGuides.FindAsync(id);
        if (ag == null) return NotFound();
        AudioGuide = ag;
        await LoadLocations();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("AudioGuide.Location");
        if (!ModelState.IsValid)
        {
            await LoadLocations();
            return Page();
        }

        _db.AudioGuides.Update(AudioGuide);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật audio guide \"{AudioGuide.Title}\"";
        return RedirectToPage("Index");
    }

    private async Task LoadLocations()
    {
        LocationList = await _db.Locations
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem { Value = l.Id, Text = l.Name })
            .ToListAsync();
    }
}
