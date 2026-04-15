using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class EditModel : PageModel
{
    private const string TtsOnApprovalField = "__tts_on_approval";

    private readonly AppDbContext _db;
    private readonly IAudioStorageService _audioStorageService;
    private readonly IPoiChangeRequestService _changeRequestService;

    public EditModel(
        AppDbContext db,
        IAudioStorageService audioStorageService,
        IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _audioStorageService = audioStorageService;
        _changeRequestService = changeRequestService;
    }

    [BindProperty]
    public AudioGuide AudioGuide { get; set; } = new();

    [BindProperty]
    public IFormFile? AudioFile { get; set; }

    [BindProperty]
    public string AudioMode { get; set; } = "upload";

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var entity = await _db.AudioGuides.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, entity.LocationId))
        {
            return Forbid();
        }

        AudioGuide = entity;
        AudioMode = entity.GeneratedFromTts ? "tts" : "upload";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("AudioGuide.Location");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var entity = await _db.AudioGuides
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == AudioGuide.Id);
        if (entity is null)
        {
            return NotFound();
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, entity.LocationId))
        {
            return Forbid();
        }

        var changedFields = new Dictionary<string, string?>();
        AddIfChanged(changedFields, nameof(AudioGuide.Title), entity.Title, AudioGuide.Title);
        AddIfChanged(changedFields, nameof(AudioGuide.Description), entity.Description, AudioGuide.Description);
        AddIfChanged(changedFields, nameof(AudioGuide.Duration), entity.Duration.ToString(CultureInfo.InvariantCulture), AudioGuide.Duration.ToString(CultureInfo.InvariantCulture));
        AddIfChanged(changedFields, nameof(AudioGuide.Language), entity.Language, AudioGuide.Language);
        AddIfChanged(changedFields, nameof(AudioGuide.AudioUrl), entity.AudioUrl, AudioGuide.AudioUrl);
        AddIfChanged(changedFields, nameof(AudioGuide.TranscriptText), entity.TranscriptText, AudioGuide.TranscriptText);

        if (string.Equals(AudioMode, "tts", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(AudioGuide.TranscriptText))
            {
                ModelState.AddModelError(nameof(AudioGuide.TranscriptText), "Vui lòng nhập transcript để tạo TTS.");
                return Page();
            }

            var transcript = AudioGuide.TranscriptText.Trim();
            AddIfChanged(changedFields, nameof(AudioGuide.GeneratedFromTts), entity.GeneratedFromTts.ToString(), bool.TrueString);
            AddIfChanged(changedFields, nameof(AudioGuide.TtsSourceText), entity.TtsSourceText, transcript);
            changedFields[TtsOnApprovalField] = bool.TrueString;
        }
        else if (AudioFile is not null)
        {
            try
            {
                var uploadResult = await _audioStorageService.UploadAudioAsync(AudioFile, entity.Id);

                AddIfChanged(changedFields, nameof(AudioGuide.AudioUrl), entity.AudioUrl, uploadResult.AudioUrl);
                AddIfChanged(changedFields, nameof(AudioGuide.CloudinaryAudioUrl), entity.CloudinaryAudioUrl, uploadResult.CloudinaryAudioUrl);
                AddIfChanged(changedFields, nameof(AudioGuide.CloudinaryPublicId), entity.CloudinaryPublicId, uploadResult.CloudinaryPublicId);
                AddIfChanged(changedFields, nameof(AudioGuide.GeneratedFromTts), entity.GeneratedFromTts.ToString(), bool.FalseString);
                AddIfChanged(changedFields, nameof(AudioGuide.TtsSourceText), entity.TtsSourceText, null);
                changedFields[TtsOnApprovalField] = bool.FalseString;

                AudioGuide.AudioUrl = uploadResult.AudioUrl;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi upload audio: {ex.Message}");
                return Page();
            }
        }

        if (changedFields.Count == 0)
        {
            TempData["Success"] = "Không có thay đổi mới để gửi duyệt.";
            return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = entity.LocationId });
        }

        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.Identity?.Name
                       ?? string.Empty;
        username = username.Trim();
        var displayName = (User.Identity?.Name ?? username).Trim();

        var changeSet = new PoiChangeSet { Fields = changedFields };

        var createdRequest = await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = displayName,
            LocationId = entity.LocationId,
            LocationName = (await _db.Locations.AsNoTracking().Where(item => item.Id == entity.LocationId).Select(item => item.Name).FirstOrDefaultAsync()) ?? entity.LocationId,
            Topic = string.Equals(AudioMode, "tts", StringComparison.OrdinalIgnoreCase)
                ? "Cập nhật audio bằng TTS"
                : "Cập nhật audio guide",
            Title = $"Cập nhật audio: {entity.Title}",
            Details = $"Đề xuất cập nhật {changedFields.Count} trường dữ liệu audio guide.",
            TargetType = PoiChangeTargetType.AudioGuide,
            TargetEntityId = entity.Id,
            ChangeSetJson = JsonSerializer.Serialize(changeSet)
        });

        TempData["Success"] = $"Đã gửi yêu cầu cập nhật audio cho Admin Hệ thống duyệt. Mã yêu cầu: {createdRequest.Id}";
        return RedirectToPage("/Shop/ChangeRequests");
    }

    private static void AddIfChanged(IDictionary<string, string?> changes, string key, string? original, string? incoming)
    {
        if (!string.Equals((original ?? string.Empty).Trim(), (incoming ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            changes[key] = incoming;
        }
    }
}
