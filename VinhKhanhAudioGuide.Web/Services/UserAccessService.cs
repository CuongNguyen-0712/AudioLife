using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Services;

public static class UserAccessService
{
    public static bool IsSystemAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.SystemAdmin);
    }

    public static bool IsPoiAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleNames.PoiAdmin);
    }

    public static HashSet<string> GetOwnedLocationIds(ClaimsPrincipal user)
    {
        return user.FindAll("owned_location")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<HashSet<string>> GetOwnedLocationIdsAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var username = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(username))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var assignedLocationIds = await db.PoiAdminLocationAssignments
            .AsNoTracking()
            .Where(item => item.Username == username)
            .Select(item => item.LocationId)
            .ToListAsync();

        if (assignedLocationIds.Count > 0)
        {
            return assignedLocationIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return GetOwnedLocationIds(user);
    }

    public static bool CanAccessLocation(ClaimsPrincipal user, string locationId)
    {
        if (!IsPoiAdmin(user))
        {
            return false;
        }

        return GetOwnedLocationIds(user).Contains(locationId);
    }

    public static async Task<bool> CanAccessLocationAsync(ClaimsPrincipal user, AppDbContext db, string locationId)
    {
        if (!IsPoiAdmin(user))
        {
            return false;
        }

        var owned = await GetOwnedLocationIdsAsync(user, db);
        return owned.Contains(locationId);
    }
}
