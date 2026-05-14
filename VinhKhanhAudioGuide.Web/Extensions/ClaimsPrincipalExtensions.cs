using System.Security.Claims;

namespace VinhKhanhAudioGuide.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        if (user == null) return null;

        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return null;
        }

        return userId;
    }
}
