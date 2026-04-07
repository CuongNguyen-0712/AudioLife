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

    [BindProperty]
    public string? TtsText { get; set; }

    public List<SelectListItem> LocationList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var ag = await _db.AudioGuides.FindAsync(id);
        if (ag == null) return NotFound();
        AudioGuide = ag;

        // Pre-fill TTS mode if it was previously generated from TTS
        if (ag.GeneratedFromTts)
        {
            AudioMode = "tts";
            TtsText = ag.TtsSourceText;
        }

        await LoadLocations();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("AudioGuide.Location");
        ModelState.Remove("AudioGuide.AudioUrl");
        ModelState.Remove("AudioGuide.Description");
        ModelState.Remove("AudioGuide.TranscriptText");
        ModelState.Remove("TtsText");

        if (!ModelState.IsValid)
        {
            await LoadLocations();
            return Page();
        }

        var existingGuide = await _db.AudioGuides.FindAsync(AudioGuide.Id);
        if (existingGuide == null) return NotFound();

        if (AudioMode == "tts" && string.IsNullOrWhiteSpace(TtsText))
        {
            ModelState.AddModelError(nameof(TtsText), "Vui lòng nhập văn bản để tạo audio bằng TTS.");
            await LoadLocations();
            return Page();
        }

        if (AudioMode == "tts" && !string.IsNullOrWhiteSpace(TtsText))
        {
            try
            {
                _logger.LogInformation("AudioGuide {Id}: Generating TTS audio in language '{Language}'", AudioGuide.Id, AudioGuide.Language);
                var audioBytes = await _ttsService.SynthesizeAsync(TtsText, AudioGuide.Language);

                if (!IsMp3OrWav(audioBytes))
                {
                    ModelState.AddModelError(string.Empty, "TTS không trả về định dạng MP3/WAV hợp lệ. Vui lòng thử lại hoặc đổi ngôn ngữ giọng đọc.");
                    _logger.LogWarning("AudioGuide {Id}: TTS returned unsupported format. Bytes: {Length}", AudioGuide.Id, audioBytes.Length);
                    await LoadLocations();
                    return Page();
                }

                using var stream = new MemoryStream(audioBytes);
                var fileExtension = GetAudioFileExtension(audioBytes);
                var fileName = $"tts_{AudioGuide.Language}_{Guid.NewGuid():N}.{fileExtension}";

                var uploadResult = await _audioStorageService.UploadAudioAsync(
                    stream, fileName, AudioGuide.Id);

                existingGuide.AudioUrl = uploadResult.AudioUrl;
                existingGuide.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                existingGuide.CloudinaryPublicId = uploadResult.CloudinaryPublicId;

                existingGuide.GeneratedFromTts = true;
                existingGuide.TtsSourceText = TtsText;
                existingGuide.TranscriptText = TtsText;
                _logger.LogInformation("AudioGuide {Id}: TTS upload success. PublicId: {PublicId}", AudioGuide.Id, uploadResult.CloudinaryPublicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AudioGuide {Id}: TTS/upload failed", AudioGuide.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadLocations();
                return Page();
            }
        }
        else if (AudioFile is not null)
        {
            try
            {
                var uploadResult = await _audioStorageService.UploadAudioAsync(AudioFile, AudioGuide.Id);
                existingGuide.AudioUrl = uploadResult.AudioUrl;
                existingGuide.CloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                existingGuide.CloudinaryPublicId = uploadResult.CloudinaryPublicId;
                existingGuide.GeneratedFromTts = false;
                existingGuide.TtsSourceText = null;
                _logger.LogInformation("AudioGuide {Id}: File upload success. PublicId: {PublicId}", AudioGuide.Id, uploadResult.CloudinaryPublicId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AudioGuide {Id}: File upload failed", AudioGuide.Id);
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadLocations();
                return Page();
            }
        }
        else
        {
            // Just update basic properties, don't overwrite AudioUrl unless user explicitly modified it
            existingGuide.AudioUrl = AudioGuide.AudioUrl;
        }

        existingGuide.LocationId = AudioGuide.LocationId;
        existingGuide.Title = AudioGuide.Title;
        existingGuide.Description = AudioGuide.Description;
        existingGuide.Duration = AudioGuide.Duration;
        existingGuide.Language = AudioGuide.Language;

        // If not using TTS, update transcript from form (so user editing isn't lost)
        if (!(AudioMode == "tts" && !string.IsNullOrWhiteSpace(TtsText)))
        {
            existingGuide.TranscriptText = AudioGuide.TranscriptText;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioGuide {Id}: SaveChanges failed", AudioGuide.Id);
            ModelState.AddModelError(string.Empty, $"Không thể cập nhật dữ liệu vào DB: {ex.Message}");
            await LoadLocations();
            return Page();
        }

        TempData["Success"] = $"Đã cập nhật audio guide \"{existingGuide.Title}\"";
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
