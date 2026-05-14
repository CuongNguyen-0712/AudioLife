using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VinhKhanhAudioGuide.Web.Configuration;

namespace VinhKhanhAudioGuide.Web.Services;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string username, string role, IEnumerable<string>? locationIds = null);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions _options;

    public JwtTokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(string userId, string username, string role, IEnumerable<string>? locationIds = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role)
        };

        if (locationIds != null)
        {
            foreach (var locId in locationIds)
            {
                claims.Add(new Claim("location_id", locId));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(_options.JwtExpiryDays > 0 ? _options.JwtExpiryDays : 7);

        var token = new JwtSecurityToken(
            _options.JwtIssuer,
            _options.JwtAudience,
            claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
