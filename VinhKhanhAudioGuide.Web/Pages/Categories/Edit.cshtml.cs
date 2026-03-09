using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Categories;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Category Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        Category = cat;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.Categories.Update(Category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật danh mục \"{Category.Name}\"";
        return RedirectToPage("Index");
    }
}
