using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public EditModel(
        AppDbContext db,
        IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
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

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, entity.LocationId))
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
        ModelState.Remove("AudioGuide.TranscriptText");
        ModelState.Remove("AudioGuide.Description");
        ModelState.Remove("AudioGuide.Duration");
        ModelState.Remove("AudioGuide.Language");

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
        AddIfChanged(changedFields, nameof(AudioGuide.TranscriptText), entity.TranscriptText, AudioGuide.TranscriptText);

        if (changedFields.Count == 0)
        {
            TempData["Success"] = "Không có thay đổi mới để gửi duyệt.";
            return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = entity.LocationId });
        }

        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.Identity?.Name
                       ?? string.Empty;

        var changeSet = new PoiChangeSet { Fields = changedFields };

        await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = User.Identity?.Name ?? username,
            LocationId = entity.LocationId,
            LocationName = (await _db.Locations.AsNoTracking().Where(item => item.Id == entity.LocationId).Select(item => item.Name).FirstOrDefaultAsync()) ?? entity.LocationId,
            Topic = "Transcript audio",
            Title = $"Cập nhật transcript: {entity.Title}",
            Details = "Đề xuất cập nhật transcript audio guide.",
            TargetType = PoiChangeTargetType.AudioGuide,
            TargetEntityId = entity.Id,
            ChangeSetJson = JsonSerializer.Serialize(changeSet)
        });

        TempData["Success"] = "Đã gửi yêu cầu cập nhật audio cho Admin Hệ thống duyệt.";
        return RedirectToPage("/Shop/AudioGuides/Index", new { locationId = entity.LocationId });
    }

    private static void AddIfChanged(IDictionary<string, string?> changes, string key, string? original, string? incoming)
    {
        if (!string.Equals((original ?? string.Empty).Trim(), (incoming ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            changes[key] = incoming;
        }
    }
}
