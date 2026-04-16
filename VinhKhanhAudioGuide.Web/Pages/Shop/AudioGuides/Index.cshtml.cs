using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.AudioGuides;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public IndexModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public List<AudioGuide> AudioGuides { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);
        IQueryable<Location> locationQuery = _db.Locations
            .AsNoTracking()
            .Where(location => ownedLocationIds.Contains(location.Id));

        AccessibleLocations = await locationQuery.OrderBy(location => location.Name).ToListAsync();

        if (!AccessibleLocations.Any())
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(LocationId))
        {
            LocationId = AccessibleLocations[0].Id;
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, LocationId))
        {
            return Forbid();
        }

        AudioGuides = await _db.AudioGuides
            .AsNoTracking()
            .Where(audioGuide => audioGuide.LocationId == LocationId)
            .OrderBy(audioGuide => audioGuide.Title)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var audioGuide = await _db.AudioGuides.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (audioGuide is null)
        {
            return NotFound();
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, audioGuide.LocationId))
        {
            return Forbid();
        }

        var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.Identity?.Name
                       ?? string.Empty;
        username = username.Trim();
        var displayName = (User.Identity?.Name ?? username).Trim();

        var locationName = await _db.Locations
            .AsNoTracking()
            .Where(item => item.Id == audioGuide.LocationId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? audioGuide.LocationId;

        var request = await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = displayName,
            LocationId = audioGuide.LocationId,
            LocationName = locationName,
            Topic = "Xóa audio guide",
            Title = $"Xóa audio: {audioGuide.Title}",
            Details = "POI Admin đề xuất xóa audio guide và chờ Admin Hệ thống duyệt.",
            TargetType = PoiChangeTargetType.AudioGuide,
            TargetEntityId = audioGuide.Id,
            ChangeSetJson = JsonSerializer.Serialize(new PoiChangeSet
            {
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["__action"] = "delete-audio-guide"
                }
            })
        });

        TempData["Success"] = $"Đã gửi yêu cầu xóa audio cho Admin Hệ thống duyệt. Mã yêu cầu: {request.Id}";
        return RedirectToPage("/Shop/ChangeRequests");
    }
}
