using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var entity = await _db.AudioGuides.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        if (!UserAccessService.CanAccessLocation(User, entity.LocationId))
        {
            return Forbid();
        }

        AudioGuide = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("AudioGuide.Location");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entity = await _db.AudioGuides.FirstOrDefaultAsync(item => item.Id == AudioGuide.Id);
        if (entity is null)
        {
            return NotFound();
        }

        if (!UserAccessService.CanAccessLocation(User, entity.LocationId))
        {
            return Forbid();
        }

        entity.Title = AudioGuide.Title;
        entity.Description = AudioGuide.Description;
        entity.AudioUrl = AudioGuide.AudioUrl;
        entity.Duration = AudioGuide.Duration;
        entity.Language = AudioGuide.Language;
        entity.TranscriptText = AudioGuide.TranscriptText;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật audio guide.";
        return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = entity.LocationId });
    }
}
