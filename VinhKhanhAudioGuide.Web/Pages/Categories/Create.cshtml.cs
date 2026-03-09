using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Categories;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Category Category { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.Categories.Add(Category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm danh mục \"{Category.Name}\"";
        return RedirectToPage("Index");
    }
}
