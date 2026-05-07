using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IAuthUserStore
{
    Task<AuthenticatedUser?> FindByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}

public class AuthUserStore : IAuthUserStore
{
    private readonly AppDbContext _db;

    public AuthUserStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthenticatedUser?> FindByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // Xác thực tài khoản web admin theo username/password và role.
        // Thuộc flow login + phân quyền SystemAdmin/PoiAdmin.
        var normalizedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var dbUser = await _db.AuthUserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.IsActive
                && user.Username == normalizedUsername
                && user.Password == password,
                cancellationToken);

        if (dbUser is null)
        {
            return null;
        }

        var normalizedRole = NormalizeRole(dbUser.Role);
        if (normalizedRole is null)
        {
            return null;
        }

        var locationIds = new List<string>();
        if (RoleNames.IsPoiAdmin(normalizedRole))
        {
            locationIds = await _db.PoiAdminLocationAssignments
                .AsNoTracking()
                .Where(item => item.Username == dbUser.Username)
                .Select(item => item.LocationId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        return new AuthenticatedUser
        {
            Username = dbUser.Username,
            DisplayName = dbUser.DisplayName,
            Role = normalizedRole,
            LocationIds = locationIds
        };
    }

    private static string? NormalizeRole(string? role)
    {
        if (RoleNames.IsSystemAdmin(role))
        {
            return RoleNames.SystemAdmin;
        }

        if (RoleNames.IsPoiAdmin(role))
        {
            return RoleNames.PoiAdmin;
        }

        return null;
    }
}

public sealed class AuthenticatedUser
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> LocationIds { get; set; } = new();
}
