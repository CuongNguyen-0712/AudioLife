namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Service để format khoảng cách theo quy tắc:
/// - < 1000m: hiển thị meter (m)
/// - >= 1000m: chuyển sang km, làm tròn 1 chữ số thập phân
/// - Loại bỏ .0 không cần thiết (10.0 km -> 10 km)
/// </summary>
public static class DistanceFormatService
{
    public static string FormatDistance(double meters)
    {
        if (meters < 1000)
        {
            // Hiển thị meter, làm tròn số nguyên
            return $"{Math.Round(meters):F0} m";
        }

        // Chuyển sang km, làm tròn 1 chữ số thập phân
        var km = meters / 1000.0;
        var kmFormatted = Math.Round(km, 1);

        // Loại bỏ .0 không cần thiết
        return kmFormatted % 1 == 0
            ? $"{(int)kmFormatted} km"
            : $"{kmFormatted} km";
    }

    public static string FormatDistance(double? meters)
    {
        return meters.HasValue ? FormatDistance(meters.Value) : string.Empty;
    }
}
