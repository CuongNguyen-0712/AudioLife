using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.Locations;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public EditModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    [BindProperty]
    public Location Location { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
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

        Location = location;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Location.Category");
        ModelState.Remove("Location.AudioGuides");
        ModelState.Remove("Location.TourLocations");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, Location.Id))
        {
            return Forbid();
        }

        var entity = await _db.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Location.Id);
        if (entity is null)
        {
            return NotFound();
        }

        var changedFields = new Dictionary<string, string?>();
        AddIfChanged(changedFields, nameof(Location.Name), entity.Name, Location.Name);
        AddIfChanged(changedFields, nameof(Location.Description), entity.Description, Location.Description);
        AddIfChanged(changedFields, nameof(Location.Address), entity.Address, Location.Address);
        AddIfChanged(changedFields, nameof(Location.Latitude), entity.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), Location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AddIfChanged(changedFields, nameof(Location.Longitude), entity.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), Location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AddIfChanged(changedFields, nameof(Location.Duration), entity.Duration.ToString(), Location.Duration.ToString());
        AddIfChanged(changedFields, nameof(Location.ImageUrl), entity.ImageUrl, Location.ImageUrl);

        if (changedFields.Count == 0)
        {
            TempData["Success"] = "Không có thay đổi mới để gửi duyệt.";
            return RedirectToPage("/Shop/Index", new { locationId = entity.Id });
        }

        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.Identity?.Name
                       ?? string.Empty;

        var changeSet = new PoiChangeSet { Fields = changedFields };

        await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = User.Identity?.Name ?? username,
            LocationId = entity.Id,
            LocationName = entity.Name,
            Topic = "Thông tin địa điểm",
            Title = $"Cập nhật POI {entity.Name}",
            Details = $"Đề xuất cập nhật {changedFields.Count} trường dữ liệu địa điểm.",
            TargetType = PoiChangeTargetType.Location,
            TargetEntityId = entity.Id,
            ChangeSetJson = JsonSerializer.Serialize(changeSet)
        });

        TempData["Success"] = "Đã gửi yêu cầu cập nhật địa điểm cho Admin Hệ thống duyệt.";
        return RedirectToPage("/Shop/Index", new { locationId = entity.Id });
    }

    private static void AddIfChanged(IDictionary<string, string?> changes, string key, string? original, string? incoming)
    {
        if (!string.Equals((original ?? string.Empty).Trim(), (incoming ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            changes[key] = incoming;
        }
    }
}
