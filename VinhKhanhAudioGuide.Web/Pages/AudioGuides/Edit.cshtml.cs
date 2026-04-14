using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAudioStorageService _audioStorageService;
    private readonly ITextToSpeechService _ttsService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        AppDbContext db,
        IAudioStorageService audioStorageService,
        ITextToSpeechService ttsService,
        ILogger<EditModel> logger)
    {
        _db = db;
        _audioStorageService = audioStorageService;
        _ttsService = ttsService;
        _logger = logger;
    }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

    [BindProperty]
    public string AudioMode { get; set; } = "upload";

    public List<SelectListItem> LocationList { get; set; } = new();

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

    private static string GetAudioFileExtension(byte[] audioBytes)
    {
        var isWav = audioBytes.Length >= 12 &&
                    audioBytes[0] == 0x52 && audioBytes[1] == 0x49 && audioBytes[2] == 0x46 && audioBytes[3] == 0x46 &&
                    audioBytes[8] == 0x57 && audioBytes[9] == 0x41 && audioBytes[10] == 0x56 && audioBytes[11] == 0x45;

        return isWav ? "wav" : "mp3";
    }

    private static bool IsMp3OrWav(byte[] audioBytes)
    {
        if (audioBytes.Length < 4)
        {
            return false;
        }

        // MP3: ID3 header or frame sync 0xFFEx
        var isMp3 = (audioBytes[0] == 0x49 && audioBytes[1] == 0x44 && audioBytes[2] == 0x33) ||
                    (audioBytes[0] == 0xFF && (audioBytes[1] & 0xE0) == 0xE0);

        // WAV: RIFF....WAVE
        var isWav = audioBytes.Length >= 12 &&
                    audioBytes[0] == 0x52 && audioBytes[1] == 0x49 && audioBytes[2] == 0x46 && audioBytes[3] == 0x46 &&
                    audioBytes[8] == 0x57 && audioBytes[9] == 0x41 && audioBytes[10] == 0x56 && audioBytes[11] == 0x45;

        return isMp3 || isWav;
    }

    private async Task LoadLocations()
    {
        LocationList = await _db.Locations
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem { Value = l.Id, Text = l.Name })
            .ToListAsync();
    }
}
