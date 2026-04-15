using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAudioStorageService _audioStorageService;
    private readonly ITextToSpeechService _ttsService;

    public CreateModel(AppDbContext db, IAudioStorageService audioStorageService, ITextToSpeechService ttsService)
    {
        _db = db;
        _audioStorageService = audioStorageService;
        _ttsService = ttsService;
    }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

    /// <summary>
    /// "upload" or "tts"
    /// </summary>
    [BindProperty]
    public string AudioMode { get; set; } = "upload";

    public List<SelectListItem> LocationList { get; set; } = new();

    public IActionResult OnGetAsync()
    {
        TempData["Success"] = "Khu Audio hệ thống đang ở chế độ xem-only.";
        return RedirectToPage("Index");
    }

    public IActionResult OnPostAsync()
    {
        TempData["Success"] = "Khu Audio hệ thống đang ở chế độ xem-only.";
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
