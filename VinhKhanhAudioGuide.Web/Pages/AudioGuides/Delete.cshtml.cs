using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var ag = await _db.AudioGuides.FindAsync(id);
        if (ag == null) return NotFound();
        AudioGuide = ag;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ag = await _db.AudioGuides.FindAsync(AudioGuide.Id);
        if (ag == null) return NotFound();

        _db.AudioGuides.Remove(ag);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa audio guide \"{ag.Title}\"";
        return RedirectToPage("Index");
    }
}
