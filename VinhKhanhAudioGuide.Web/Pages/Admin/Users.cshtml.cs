using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthOptions _authOptions;
    private readonly IPoiAdminAssignmentService _assignmentService;

    public UsersModel(
        AppDbContext db,
        IOptions<AuthOptions> authOptions,
        IPoiAdminAssignmentService assignmentService)
    {
        _db = db;
        _authOptions = authOptions.Value;
        _assignmentService = assignmentService;
    }

    public List<UserRow> Users { get; set; } = new();
    public List<LocationAssignmentRow> LocationAssignments { get; set; } = new();
    public List<string> AllLocationIds { get; set; } = new();

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public List<string> AssignedLocationIds { get; set; } = new();

    [BindProperty]
    public string NewPoiUsername { get; set; } = string.Empty;

    [BindProperty]
    public string NewPoiDisplayName { get; set; } = string.Empty;

    [BindProperty]
    public string NewPoiPassword { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAssignmentsAsync()
    {
        var result = await _assignmentService.UpdateAssignmentsAsync(Username, AssignedLocationIds, HttpContext.RequestAborted);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Không thể cập nhật phạm vi POI.";
            return RedirectToPage();
        }

        var extraHints = new List<string>();
        if (result.TransferredLocationIds.Count > 0)
        {
            extraHints.Add($"Đã chuyển {result.TransferredLocationIds.Count} POI từ tài khoản khác sang tài khoản này");
        }

        if (result.InvalidLocationIds.Count > 0)
        {
            extraHints.Add($"Bỏ qua {result.InvalidLocationIds.Count} POI không hợp lệ");
        }

        TempData["Success"] = extraHints.Count == 0
            ? "Đã cập nhật phạm vi POI cho tài khoản."
            : $"Đã cập nhật phạm vi POI. {string.Join("; ", extraHints)}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreatePoiAdminAsync()
    {
        var username = (NewPoiUsername ?? string.Empty).Trim();
        var displayName = (NewPoiDisplayName ?? string.Empty).Trim();
        var password = (NewPoiPassword ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tài khoản, tên hiển thị và mật khẩu cho POI Admin mới.";
            return RedirectToPage();
        }

        if (username.Length > 100)
        {
            TempData["Error"] = "Tên tài khoản tối đa 100 ký tự.";
            return RedirectToPage();
        }

        if (displayName.Length > 150)
        {
            TempData["Error"] = "Tên hiển thị tối đa 150 ký tự.";
            return RedirectToPage();
        }

        var existsInConfig = _authOptions.Users.Any(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase));

        var existsInDb = await _db.AuthUserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Username == username, HttpContext.RequestAborted);

        if (existsInConfig || existsInDb)
        {
            TempData["Error"] = "Tài khoản đã tồn tại. Vui lòng chọn username khác.";
            return RedirectToPage();
        }

        _db.AuthUserAccounts.Add(new Models.AuthUserAccount
        {
            Username = username,
            DisplayName = displayName,
            Password = password,
            Role = RoleNames.PoiAdmin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        TempData["Success"] = "Đã tạo tài khoản POI Admin mới. Bạn có thể phân quyền POI ngay bên dưới.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var locationMap = await _db.Locations
            .AsNoTracking()
            .ToDictionaryAsync(location => location.Id, location => location.Name, StringComparer.OrdinalIgnoreCase);

        AllLocationIds = locationMap.Keys.OrderBy(id => id).ToList();

        var assignmentMap = await _db.PoiAdminLocationAssignments
            .AsNoTracking()
            .GroupBy(item => item.Username)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(item => item.LocationId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var dbUsers = await _db.AuthUserAccounts
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Select(user => new AuthUserOption
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                Password = user.Password,
                Role = user.Role,
                LocationIds = new List<string>()
            })
            .ToListAsync();

        var allUsers = _authOptions.Users
            .Concat(dbUsers)
            .GroupBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        Users = allUsers
            .Select((user, index) => new UserRow
            {
                No = index + 1,
                Username = user.Username,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName,
                Role = user.Role,
                ManagedLocations = (assignmentMap.TryGetValue(user.Username, out var assignedIds) ? assignedIds : user.LocationIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(id => locationMap.TryGetValue(id, out var name) ? name : id)
                    .ToList(),
                ManagedLocationIds = (assignmentMap.TryGetValue(user.Username, out var managedIds) ? managedIds : user.LocationIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                IsActive = true
            })
            .OrderBy(row => row.Role)
            .ThenBy(row => row.DisplayName)
            .ToList();

        var poiAdmins = Users
            .Where(user => string.Equals(user.Role, RoleNames.PoiAdmin, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var poiAdminNameMap = poiAdmins
            .ToDictionary(admin => admin.Username, admin => admin.DisplayName, StringComparer.OrdinalIgnoreCase);

        LocationAssignments = locationMap
            .Select(item => new LocationAssignmentRow
            {
                LocationId = item.Key,
                LocationName = item.Value,
                PoiAdminUsernames = poiAdmins
                    .Where(user => user.ManagedLocationIds.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                    .Select(user => user.Username)
                    .OrderBy(username => username)
                    .ToList(),
                PoiAdminNames = poiAdmins
                    .Where(user => user.ManagedLocationIds.Contains(item.Key, StringComparer.OrdinalIgnoreCase))
                    .Select(user => poiAdminNameMap.TryGetValue(user.Username, out var displayName) ? displayName : user.Username)
                    .OrderBy(name => name)
                    .ToList()
            })
            .OrderBy(item => item.LocationName)
            .ToList();
    }

    public class UserRow
    {
        public int No { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> ManagedLocations { get; set; } = new();
        public List<string> ManagedLocationIds { get; set; } = new();

        public string RoleLabel => Role.Equals(RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            ? "Admin Hệ Thống"
            : "Admin POI";

        public string RoleBadgeClass => Role.Equals(RoleNames.SystemAdmin, StringComparison.OrdinalIgnoreCase)
            ? "text-bg-danger"
            : "text-bg-primary";
    }

    public class LocationAssignmentRow
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public List<string> PoiAdminUsernames { get; set; } = new();
        public List<string> PoiAdminNames { get; set; } = new();
    }
}
