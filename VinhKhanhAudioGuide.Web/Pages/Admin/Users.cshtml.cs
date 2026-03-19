using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class UsersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthOptions _authOptions;

    public UsersModel(AppDbContext db, IOptions<AuthOptions> authOptions)
    {
        _db = db;
        _authOptions = authOptions.Value;
    }

    public List<UserRow> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        var locationMap = await _db.Locations
            .AsNoTracking()
            .ToDictionaryAsync(location => location.Id, location => location.Name, StringComparer.OrdinalIgnoreCase);

        Users = _authOptions.Users
            .Select((user, index) => new UserRow
            {
                No = index + 1,
                Username = user.Username,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName,
                Role = user.Role,
                ManagedLocations = user.LocationIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(id => locationMap.TryGetValue(id, out var name) ? name : id)
                    .ToList(),
                IsActive = true
            })
            .OrderBy(row => row.Role)
            .ThenBy(row => row.DisplayName)
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

        public string RoleLabel => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin Hệ Thống"
            : "Admin POI";

        public string RoleBadgeClass => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? "text-bg-danger"
            : "text-bg-primary";
    }
}
