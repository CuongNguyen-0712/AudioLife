using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop.Locations;

public class CreateModel : PageModel
{
    private const string CreateLocationAction = "create-location";

    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public CreateModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    [BindProperty]
    public CreatePoiInput Input { get; set; } = new();

    [BindProperty]
    public string CoordinateMode { get; set; } = "map";

    public List<SelectListItem> CategoryList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!UserAccessService.IsPoiAdmin(User))
        {
            return Forbid();
        }

        await LoadCategoriesAsync();

        Input.Id = $"loc_{Guid.NewGuid():N}"[..12];
        Input.LatitudeText = "10.760000";
        Input.LongitudeText = "106.704000";
        Input.Duration = 20;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!UserAccessService.IsPoiAdmin(User))
        {
            return Forbid();
        }

        await LoadCategoriesAsync();

        if (!TryParseCoordinate(Input.LatitudeText, out var latitude))
        {
            ModelState.AddModelError(nameof(Input.LatitudeText), "Vĩ độ không hợp lệ.");
        }

        if (!TryParseCoordinate(Input.LongitudeText, out var longitude))
        {
            ModelState.AddModelError(nameof(Input.LongitudeText), "Kinh độ không hợp lệ.");
        }

        if (latitude < -90 || latitude > 90)
        {
            ModelState.AddModelError(nameof(Input.LatitudeText), "Vĩ độ phải nằm trong khoảng -90 đến 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            ModelState.AddModelError(nameof(Input.LongitudeText), "Kinh độ phải nằm trong khoảng -180 đến 180.");
        }

        Input.Id = Input.Id.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var locationExists = await _db.Locations
            .AsNoTracking()
            .AnyAsync(item => item.Id == Input.Id);

        if (locationExists)
        {
            ModelState.AddModelError(nameof(Input.Id), "Mã POI đã tồn tại.");
            return Page();
        }

        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(item => item.Id == Input.CategoryId);

        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(Input.CategoryId), "Danh mục không tồn tại.");
            return Page();
        }

        var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.Identity?.Name
                       ?? string.Empty;
        username = username.Trim();
        var displayName = (User.Identity?.Name ?? username).Trim();

        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["__action"] = CreateLocationAction,
            [nameof(Location.Name)] = Input.Name.Trim(),
            [nameof(Location.Description)] = Input.Description?.Trim() ?? string.Empty,
            [nameof(Location.Address)] = Input.Address?.Trim() ?? string.Empty,
            [nameof(Location.ImageUrl)] = Input.ImageUrl?.Trim() ?? string.Empty,
            [nameof(Location.CategoryId)] = Input.CategoryId.Trim(),
            [nameof(Location.Latitude)] = latitude.ToString(CultureInfo.InvariantCulture),
            [nameof(Location.Longitude)] = longitude.ToString(CultureInfo.InvariantCulture),
            [nameof(Location.Duration)] = Math.Max(0, Input.Duration).ToString(CultureInfo.InvariantCulture)
        };

        var createdRequest = await _changeRequestService.SubmitAsync(new PoiChangeRequest
        {
            SubmittedByUsername = username,
            SubmittedByName = displayName,
            LocationId = Input.Id,
            LocationName = Input.Name.Trim(),
            Topic = "Tạo địa điểm mới",
            Title = $"Tạo POI mới: {Input.Name}",
            Details = string.Equals(CoordinateMode, "map", StringComparison.OrdinalIgnoreCase)
                ? "POI Admin đề xuất tạo địa điểm mới với tọa độ chọn trên bản đồ."
                : "POI Admin đề xuất tạo địa điểm mới với tọa độ nhập tay.",
            TargetType = PoiChangeTargetType.Location,
            TargetEntityId = Input.Id,
            ChangeSetJson = JsonSerializer.Serialize(new PoiChangeSet { Fields = fields })
        });

        TempData["Success"] = $"Đã gửi yêu cầu tạo POI cho Admin Hệ thống duyệt. Mã yêu cầu: {createdRequest.Id}";
        return RedirectToPage("/Shop/ChangeRequests");
    }

    private async Task LoadCategoriesAsync()
    {
        CategoryList = await _db.Categories
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new SelectListItem
            {
                Value = item.Id,
                Text = item.Name
            })
            .ToListAsync();
    }

    private static bool TryParseCoordinate(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            || double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
    }

    public class CreatePoiInput
    {
        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CategoryId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public string LatitudeText { get; set; } = string.Empty;

        [Required]
        public string LongitudeText { get; set; } = string.Empty;

        [Range(0, 600)]
        public int Duration { get; set; }
    }
}
