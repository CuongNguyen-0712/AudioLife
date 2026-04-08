using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IAuthUserStore
{
    Task<AuthUserOption?> FindByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}

public class AuthUserStore : IAuthUserStore
{
    private readonly List<AuthUserOption> _users;
    private readonly AppDbContext _db;

    public AuthUserStore(IOptions<AuthOptions> options, AppDbContext db)
    {
        _users = options.Value.Users;
        _db = db;
    }

    public async Task<AuthUserOption?> FindByCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var configuredUser = _users.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
            && user.Password == password);

        if (configuredUser is not null)
        {
            return configuredUser;
        }

        var dbUser = await _db.AuthUserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.IsActive
                && user.Username == username
                && user.Password == password,
                cancellationToken);

        if (dbUser is null)
        {
            return null;
        }

        var locationIds = await _db.PoiAdminLocationAssignments
            .AsNoTracking()
            .Where(item => item.Username == dbUser.Username)
            .Select(item => item.LocationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new AuthUserOption
        {
            Username = dbUser.Username,
            Password = dbUser.Password,
            DisplayName = dbUser.DisplayName,
            Role = dbUser.Role,
            LocationIds = locationIds
        };
    }
}
