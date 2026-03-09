namespace VinhKhanhAudioGuide.Mobile.Services;

public class NearbyLocationEventArgs : EventArgs
{
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
}

public interface IGeolocationService
{
    event EventHandler<NearbyLocationEventArgs>? NearbyLocationDetected;

    double? CurrentLatitude { get; }
    double? CurrentLongitude { get; }

    Task<bool> RequestPermissionAsync();
    Task StartTrackingAsync();
    void StopTracking();
    Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync();
}
