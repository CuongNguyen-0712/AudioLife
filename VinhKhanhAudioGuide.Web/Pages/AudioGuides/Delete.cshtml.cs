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

    public IActionResult OnGetAsync(string id)
    {
        TempData["Success"] = "Khu Audio hệ thống đang ở chế độ xem-only.";
        return RedirectToPage("Index");
    }

    public IActionResult OnPostAsync()
    {
        TempData["Success"] = "Khu Audio hệ thống đang ở chế độ xem-only.";
        return RedirectToPage("Index");
    }
}
