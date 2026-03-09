using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Locations;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Location Location { get; set; } = new();

    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var loc = await _db.Locations.FindAsync(id);
        if (loc == null) return NotFound();
        Location = loc;
        await LoadCategories();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Location.Category");
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return Page();
        }

        _db.Locations.Update(Location);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật địa điểm \"{Location.Name}\"";
        return RedirectToPage("Index");
    }

    private async Task LoadCategories()
    {
        CategoryList = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Id, Text = c.Name })
            .ToListAsync();
    }
}
