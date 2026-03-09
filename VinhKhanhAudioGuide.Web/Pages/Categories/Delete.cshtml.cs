using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Categories;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Category Category { get; set; } = new();
    public int LocationCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        Category = cat;
        LocationCount = await _db.Locations.CountAsync(l => l.CategoryId == id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var cat = await _db.Categories.FindAsync(Category.Id);
        if (cat == null) return NotFound();

        var hasLocations = await _db.Locations.AnyAsync(l => l.CategoryId == cat.Id);
        if (hasLocations)
        {
            TempData["Success"] = "Không thể xóa danh mục đang có địa điểm liên kết.";
            return RedirectToPage("Index");
        }

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa danh mục \"{cat.Name}\"";
        return RedirectToPage("Index");
    }
}
