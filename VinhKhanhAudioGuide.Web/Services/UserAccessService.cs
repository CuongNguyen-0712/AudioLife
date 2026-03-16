using System.Security.Claims;

namespace VinhKhanhAudioGuide.Web.Services;

public static class UserAccessService
{
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }

    public static HashSet<string> GetOwnedLocationIds(ClaimsPrincipal user)
    {
        return user.FindAll("owned_location")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static bool CanAccessLocation(ClaimsPrincipal user, string locationId)
    {
        if (IsAdmin(user))
        {
            return true;
        }

        return GetOwnedLocationIds(user).Contains(locationId);
    }
}
