using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IAuthUserStore
{
    AuthUserOption? FindByCredentials(string username, string password);
}

public class AuthUserStore : IAuthUserStore
{
    private readonly List<AuthUserOption> _users;

    public AuthUserStore(IOptions<AuthOptions> options)
    {
        _users = options.Value.Users;
    }

    public AuthUserOption? FindByCredentials(string username, string password)
    {
        return _users.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)
            && user.Password == password);
    }
}
