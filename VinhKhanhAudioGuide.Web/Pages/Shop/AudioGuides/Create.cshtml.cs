using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAudioStorageService _audioStorageService;
    private readonly ITextToSpeechService _ttsService;
    private readonly IPoiChangeRequestService _changeRequestService;

    public CreateModel(
        AppDbContext db,
        IAudioStorageService audioStorageService,
        ITextToSpeechService ttsService,
        IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _audioStorageService = audioStorageService;
        _ttsService = ttsService;
        _changeRequestService = changeRequestService;
    }

    [BindProperty]
    public CreateAudioInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

    [BindProperty]
    public string AudioMode { get; set; } = "upload";

    public List<SelectListItem> LocationList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? locationId)
    {
        await LoadLocationsAsync();

        if (!LocationList.Any())
        {
            return Page();
        }

        var firstLocationId = LocationList[0].Value;
        Input.LocationId = string.IsNullOrWhiteSpace(locationId) ? firstLocationId : locationId;
        Input.Id = $"ag_{Guid.NewGuid():N}"[..15];
        Input.Language = "vi";

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, Input.LocationId))
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLocationsAsync();

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, Input.LocationId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(Input.Id))
        {
            Input.Id = $"ag_{Guid.NewGuid():N}"[..15];
        }

        Input.Id = Input.Id.Trim();

        var idExists = await _db.AudioGuides
            .AsNoTracking()
            .AnyAsync(item => item.Id == Input.Id);

        if (idExists)
        {
            ModelState.AddModelError(nameof(Input.Id), "Mã audio đã tồn tại.");
        }

        string? finalAudioUrl = string.IsNullOrWhiteSpace(Input.AudioUrl) ? null : Input.AudioUrl.Trim();
        string? cloudinaryAudioUrl = null;
        string? cloudinaryPublicId = null;
        bool generatedFromTts = false;
        string? ttsSourceText = null;

        if (string.Equals(AudioMode, "tts", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Input.TranscriptText))
            {
                ModelState.AddModelError(nameof(Input.TranscriptText), "Vui lòng nhập transcript để tạo TTS.");
            }
            else
            {
                try
                {
                    var ttsText = Input.TranscriptText.Trim();
                    var audioBytes = await _ttsService.SynthesizeAsync(ttsText, Input.Language);
                    using var stream = new MemoryStream(audioBytes);

                    var uploadResult = await _audioStorageService.UploadAudioAsync(
                        stream,
                        $"tts_{Input.Language}_{Guid.NewGuid():N}.mp3",
                        Input.Id);

                    finalAudioUrl = uploadResult.AudioUrl;
                    cloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                    cloudinaryPublicId = uploadResult.CloudinaryPublicId;
                    generatedFromTts = true;
                    ttsSourceText = ttsText;
                    Input.TranscriptText = ttsText;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Lỗi tạo audio TTS: {ex.Message}");
                }
            }
        }
        else
        {
            if (AudioFile is not null)
            {
                try
                {
                    var uploadResult = await _audioStorageService.UploadAudioAsync(AudioFile, Input.Id);
                    finalAudioUrl = uploadResult.AudioUrl;
                    cloudinaryAudioUrl = uploadResult.CloudinaryAudioUrl;
                    cloudinaryPublicId = uploadResult.CloudinaryPublicId;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(finalAudioUrl))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng upload file audio hoặc nhập Audio URL.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var locationName = await _db.Locations
            .AsNoTracking()
            .Where(item => item.Id == Input.LocationId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? Input.LocationId;

        var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.Identity?.Name
                       ?? string.Empty;

        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["__action"] = "create-audio-guide",
            [nameof(AudioGuide.Title)] = Input.Title?.Trim(),
            [nameof(AudioGuide.Description)] = Input.Description?.Trim() ?? string.Empty,
            [nameof(AudioGuide.Duration)] = Math.Max(0, Input.Duration).ToString(),
            [nameof(AudioGuide.Language)] = string.IsNullOrWhiteSpace(Input.Language) ? "vi" : Input.Language.Trim(),
            [nameof(AudioGuide.TranscriptText)] = Input.TranscriptText ?? string.Empty,
            [nameof(AudioGuide.AudioUrl)] = finalAudioUrl,
            [nameof(AudioGuide.CloudinaryAudioUrl)] = cloudinaryAudioUrl,
            [nameof(AudioGuide.CloudinaryPublicId)] = cloudinaryPublicId,
            [nameof(AudioGuide.GeneratedFromTts)] = generatedFromTts.ToString(),
            [nameof(AudioGuide.TtsSourceText)] = ttsSourceText
        };

        var requestTopic = string.Equals(AudioMode, "tts", StringComparison.OrdinalIgnoreCase)
            ? "Thêm audio mới (TTS)"
            : "Thêm audio mới (Upload)";

        await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = User.Identity?.Name ?? username,
            LocationId = Input.LocationId,
            LocationName = locationName,
            Topic = requestTopic,
            Title = $"Thêm audio mới: {Input.Title}",
            Details = "POI Admin đề xuất tạo mới audio guide và chờ Admin Hệ thống duyệt.",
            TargetType = PoiChangeTargetType.AudioGuide,
            TargetEntityId = Input.Id,
            ChangeSetJson = JsonSerializer.Serialize(new PoiChangeSet { Fields = fields })
        });

        TempData["Success"] = "Đã gửi yêu cầu thêm audio cho Admin Hệ thống duyệt.";
        return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = Input.LocationId });
    }

    private async Task LoadLocationsAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);

        LocationList = await _db.Locations
            .AsNoTracking()
            .Where(item => ownedLocationIds.Contains(item.Id))
            .OrderBy(item => item.Name)
            .Select(item => new SelectListItem
            {
                Value = item.Id,
                Text = item.Name
            })
            .ToListAsync();
    }

    public class CreateAudioInput
    {
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LocationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        public string TranscriptText { get; set; } = string.Empty;

        [Range(0, 600)]
        public int Duration { get; set; }

        [Required]
        [MaxLength(10)]
        public string Language { get; set; } = "vi";
    }
}
