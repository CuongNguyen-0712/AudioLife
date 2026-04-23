using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class GeolocationService : IGeolocationService, IDisposable
{
    private const int TrackingIntervalSeconds = 5; // Reduced from 60 to support precise proximity detection
    private const double MinimumScanRadiusMeters = 50;
    private const double MaximumScanRadiusMeters = 150;
    private const double AccuracyRadiusMultiplier = 1.5;
    private const double DistanceTieThresholdMeters = 5;
    private readonly IApiService _apiService;
    private CancellationTokenSource? _cts;
    private bool _isTracking;
    private Location? _latestLocation;

    public GeolocationService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public event EventHandler<NearbyLocationEventArgs>? NearbyLocationDetected;
    public double? CurrentLatitude => _latestLocation?.Latitude;
    public double? CurrentLongitude => _latestLocation?.Longitude;
    public Location? LatestLocation => _latestLocation;

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
        }
        catch
        {
            return false;
        }
    }

    public async Task StartTrackingAsync()
    {
        if (_isTracking) return;

        var granted = await RequestPermissionAsync();
        if (!granted) return;

        _isTracking = true;
        _cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)),
                        _cts.Token);

                    if (location != null)
                    {
                        _latestLocation = location;
                        await CheckNearbyLocationsAsync(location.Latitude, location.Longitude, location.Accuracy);
                    }
                }
                catch (FeatureNotSupportedException)
                {
                    StopTracking();
                    return;
                }
                catch (PermissionException)
                {
                    StopTracking();
                    return;
                }
                catch (Exception)
                {
                    // GPS temporarily unavailable, retry next cycle
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(TrackingIntervalSeconds), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        });
    }

    public void StopTracking()
    {
        _isTracking = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
    {
        try
        {
            var granted = await RequestPermissionAsync();
            if (!granted) return null;

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (location != null)
            {
                _latestLocation = location;
                return (location.Latitude, location.Longitude);
            }
        }
        catch
        {
            // GPS unavailable
        }
        return null;
    }

    private async Task CheckNearbyLocationsAsync(double userLat, double userLng, double? locationAccuracyMeters)
    {
        var scanRadiusMeters = GetEffectiveScanRadiusMeters(locationAccuracyMeters);
        var nearbyLocations = await _apiService.GetNearbyLocationsAsync(userLat, userLng, scanRadiusMeters / 1000d);
        if (nearbyLocations.Count == 0)
        {
            NearbyLocationDetected?.Invoke(this, new NearbyLocationEventArgs { Candidates = new List<NearbyLocationCandidate>() });
            return;
        }

        var candidates = nearbyLocations
            .Select(loc => new NearbyLocationCandidate
            {
                LocationId = loc.Id,
                LocationName = loc.Name,
                DistanceMeters = CalculateDistanceKm(userLat, userLng, loc.Latitude, loc.Longitude) * 1000d,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                Priority = loc.Priority
            })
            .OrderBy(item => item.DistanceMeters)
            .ToList();

        NearbyLocationDetected?.Invoke(this, new NearbyLocationEventArgs
        {
            Candidates = candidates
        });
    }

    private static double GetEffectiveScanRadiusMeters(double? locationAccuracyMeters)
    {
        var accuracy = locationAccuracyMeters.GetValueOrDefault(MinimumScanRadiusMeters);
        var adjusted = Math.Max(MinimumScanRadiusMeters, accuracy * AccuracyRadiusMultiplier);
        return Math.Min(MaximumScanRadiusMeters, adjusted);
    }

    /// <summary>
    /// Calculate distance in km between two GPS coordinates using Haversine formula.
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        => DistanceCalculator.CalculateDistanceKm(lat1, lon1, lat2, lon2);

    public void Dispose()
    {
        StopTracking();
    }
}
