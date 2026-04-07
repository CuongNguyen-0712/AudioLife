using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class GeolocationService : IGeolocationService, IDisposable
{
    private const double NearbyRadiusKm = 0.1; // 100m radius
    private const int TrackingIntervalSeconds = 60; // 1 minute like web
    private readonly HashSet<string> _notifiedLocationIds = new();
    private CancellationTokenSource? _cts;
    private bool _isTracking;

    public event EventHandler<NearbyLocationEventArgs>? NearbyLocationDetected;
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }

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
                        CurrentLatitude = location.Latitude;
                        CurrentLongitude = location.Longitude;
                        CheckNearbyLocations(location.Latitude, location.Longitude);
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
                CurrentLatitude = location.Latitude;
                CurrentLongitude = location.Longitude;
                return (location.Latitude, location.Longitude);
            }
        }
        catch
        {
            // GPS unavailable
        }
        return null;
    }

    private void CheckNearbyLocations(double userLat, double userLng)
    {
        var locations = Data.SampleData.GetLocations();
        foreach (var loc in locations)
        {
            if (_notifiedLocationIds.Contains(loc.Id)) continue;

            var distance = CalculateDistanceKm(userLat, userLng, loc.Latitude, loc.Longitude);
            if (distance <= NearbyRadiusKm)
            {
                _notifiedLocationIds.Add(loc.Id);
                NearbyLocationDetected?.Invoke(this, new NearbyLocationEventArgs
                {
                    LocationId = loc.Id,
                    LocationName = loc.Name,
                    DistanceMeters = distance * 1000
                });
            }
        }
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public void Dispose()
    {
        StopTracking();
    }
}
