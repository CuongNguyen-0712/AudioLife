namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Utility for geographic distance calculations using Haversine formula.
/// Ensures consistent distance calculation across all services and view models.
/// </summary>
public static class DistanceCalculator
{
    /// <summary>
    /// Earth's mean radius in kilometers.
    /// </summary>
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculate distance between two GPS coordinates using Haversine formula.
    /// </summary>
    /// <param name="lat1">First point latitude (-90 to 90)</param>
    /// <param name="lon1">First point longitude (-180 to 180)</param>
    /// <param name="lat2">Second point latitude (-90 to 90)</param>
    /// <param name="lon2">Second point longitude (-180 to 180)</param>
    /// <returns>Distance in kilometers</returns>
    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLatRad = ToRadians(lat2 - lat1);
        var dLonRad = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLatRad / 2) * Math.Sin(dLatRad / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLonRad / 2) * Math.Sin(dLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Calculate distance and return in meters.
    /// </summary>
    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        return CalculateDistanceKm(lat1, lon1, lat2, lon2) * 1000.0;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
