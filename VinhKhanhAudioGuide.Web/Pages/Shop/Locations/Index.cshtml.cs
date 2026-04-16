using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.Locations;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public IndexModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    public List<LocationRow> Locations { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);

        var accessibleLocations = await _db.Locations
            .AsNoTracking()
            .Include(location => location.Category)
            .Where(location => ownedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToListAsync();

        var audioCounts = await _db.AudioGuides
            .AsNoTracking()
            .Where(audioGuide => ownedLocationIds.Contains(audioGuide.LocationId))
            .GroupBy(audioGuide => audioGuide.LocationId)
            .Select(group => new { LocationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.LocationId, item => item.Count);

        Locations = accessibleLocations
            .Select(location => new LocationRow
            {
                Location = location,
                AudioCount = audioCounts.TryGetValue(location.Id, out var audioCount) ? audioCount : 0
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        if (!await UserAccessService.CanAccessLocationAsync(User, _db, id))
        {
            return Forbid();
        }

        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (location is null)
        {
            return NotFound();
        }

        var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.Identity?.Name
                       ?? string.Empty;
        username = username.Trim();
        var displayName = (User.Identity?.Name ?? username).Trim();

        var request = await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = displayName,
            LocationId = location.Id,
            LocationName = location.Name,
            Topic = "Xóa địa điểm",
            Title = $"Xóa POI: {location.Name}",
            Details = "POI Admin đề xuất xóa địa điểm và chờ Admin Hệ thống duyệt.",
            TargetType = PoiChangeTargetType.Location,
            TargetEntityId = location.Id,
            ChangeSetJson = JsonSerializer.Serialize(new PoiChangeSet
            {
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["__action"] = "delete-location"
                }
            })
        });

        TempData["Success"] = $"Đã gửi yêu cầu xóa địa điểm cho Admin Hệ thống duyệt. Mã yêu cầu: {request.Id}";
        return RedirectToPage("/Shop/ChangeRequests");
    }

    public class LocationRow
    {
        public Location Location { get; set; } = new();
        public int AudioCount { get; set; }
    }
}