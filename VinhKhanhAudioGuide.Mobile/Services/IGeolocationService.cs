namespace VinhKhanhAudioGuide.Mobile.Services;

public class NearbyLocationCandidate
{
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Priority { get; set; } = 100;
}

public class NearbyLocationEventArgs : EventArgs
{
    public List<NearbyLocationCandidate> Candidates { get; set; } = new();
    public string BestLocationId => Candidates.FirstOrDefault()?.LocationId ?? string.Empty;
}

public interface IGeolocationService
{
    event EventHandler<NearbyLocationEventArgs>? NearbyLocationDetected;

    double? CurrentLatitude { get; }
    double? CurrentLongitude { get; }
    Microsoft.Maui.Devices.Sensors.Location? LatestLocation { get; }

    Task<bool> RequestPermissionAsync();
    Task StartTrackingAsync();
    void StopTracking();
    Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync();
}
