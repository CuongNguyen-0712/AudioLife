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

    /// <summary>
    /// Text input for TTS generation
    /// </summary>
    [BindProperty]
    public string? TtsText { get; set; }

    public List<SelectListItem> LocationList { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadLocations();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("AudioGuide.Location");
        ModelState.Remove("AudioGuide.AudioUrl");
        ModelState.Remove("AudioGuide.Description");
        ModelState.Remove("AudioGuide.TranscriptText");
        ModelState.Remove("TtsText");

        if (AudioMode == "tts" && !string.IsNullOrWhiteSpace(TtsText))
        {
            try
            {
                var audioBytes = await _ttsService.SynthesizeAsync(TtsText, AudioGuide.Language);

                using var stream = new MemoryStream(audioBytes);
                var fileName = $"tts_{AudioGuide.Language}_{Guid.NewGuid():N}.mp3";

                var uploadResult = await _audioStorageService.UploadAudioAsync(
                    stream, fileName, AudioGuide.Id ?? Guid.NewGuid().ToString("N"));

                AudioGuide.AudioUrl = uploadResult.AudioUrl;
                AudioGuide.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                AudioGuide.CloudinaryPublicId = uploadResult.CloudinaryPublicId;

                AudioGuide.GeneratedFromTts = true;
                AudioGuide.TtsSourceText = TtsText;
                AudioGuide.TranscriptText = TtsText;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tạo audio TTS: {ex.Message}");
            }
        }
        else if (AudioFile is not null)
        {
            try
            {
                var uploadResult = await _audioStorageService.UploadAudioAsync(AudioFile, AudioGuide.Id ?? Guid.NewGuid().ToString("N"));
                AudioGuide.AudioUrl = uploadResult.AudioUrl;
                AudioGuide.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                AudioGuide.CloudinaryPublicId = uploadResult.CloudinaryPublicId;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadLocations();
            return Page();
        }

        _db.AudioGuides.Add(AudioGuide);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm audio guide \"{AudioGuide.Title}\"";
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
