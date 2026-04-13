namespace VinhKhanhAudioGuide.Web.Services;

public static class RoleNames
{
    public const string SystemAdmin = "SystemAdmin";
    public const string PoiAdmin = "PoiAdmin";

    public static bool IsSystemAdmin(string? role)
    {
        return string.Equals(role, SystemAdmin, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPoiAdmin(string? role)
    {
        return string.Equals(role, PoiAdmin, StringComparison.OrdinalIgnoreCase);
    }
}
