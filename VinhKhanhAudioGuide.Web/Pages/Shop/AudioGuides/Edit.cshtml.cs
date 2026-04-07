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
    private readonly IAudioStorageService _audioStorageService;

    public EditModel(AppDbContext db, IAudioStorageService audioStorageService)
    {
        _db = db;
        _audioStorageService = audioStorageService;
    }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

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
        ModelState.Remove("AudioGuide.AudioUrl");
        ModelState.Remove("AudioGuide.Description");
        ModelState.Remove("AudioGuide.TranscriptText");

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

        if (AudioFile is not null)
        {
            try
            {
                var uploadResult = await _audioStorageService.UploadAudioAsync(AudioFile, entity.Id);
                entity.AudioUrl = uploadResult.AudioUrl;
                entity.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                entity.CloudinaryPublicId = uploadResult.CloudinaryPublicId;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
        else
        {
            entity.AudioUrl = AudioGuide.AudioUrl;
        }

        entity.Duration = AudioGuide.Duration;
        entity.Language = AudioGuide.Language;
        entity.TranscriptText = AudioGuide.TranscriptText;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật audio guide.";
        return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = entity.LocationId });
    }
}
