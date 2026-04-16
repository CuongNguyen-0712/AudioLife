using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class GeolocationService : IGeolocationService, IDisposable
{
    private const int TrackingIntervalSeconds = 60; // 1 minute like web
    private const double MinimumScanRadiusMeters = 50;
    private const double MaximumScanRadiusMeters = 150;
    private const double AccuracyRadiusMultiplier = 1.5;
    private const double DistanceTieThresholdMeters = 5;
    private readonly IApiService _apiService;
    private readonly List<string> _nearbyQueue = new();
    private CancellationTokenSource? _cts;
    private bool _isTracking;

    public GeolocationService(IApiService apiService)
    {
        _apiService = apiService;
    }

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

    private async Task CheckNearbyLocationsAsync(double userLat, double userLng, double? locationAccuracyMeters)
    {
        var scanRadiusMeters = GetEffectiveScanRadiusMeters(locationAccuracyMeters);
        var nearbyLocations = await _apiService.GetNearbyLocationsAsync(userLat, userLng, scanRadiusMeters / 1000d);
        if (nearbyLocations.Count == 0)
        {
            _nearbyQueue.Clear();
            NearbyLocationDetected?.Invoke(this, new NearbyLocationEventArgs
            {
                LocationId = string.Empty,
                LocationName = string.Empty,
                DistanceMeters = double.MaxValue
            });
            return;
        }

        var ranked = nearbyLocations
            .Select(loc => new
            {
                Location = loc,
                DistanceMeters = CalculateDistanceKm(userLat, userLng, loc.Latitude, loc.Longitude) * 1000d
            })
            .OrderBy(item => item.DistanceMeters)
            .ThenByDescending(item => item.Location.Priority)
            .ThenBy(item => item.Location.Id, StringComparer.OrdinalIgnoreCase)
            .Select(item => new NearbyCandidate(item.Location, item.DistanceMeters))
            .ToList();

        var first = ranked[0];
        var tieCandidates = ranked
            .Where(item => Math.Abs(item.DistanceMeters - first.DistanceMeters) <= DistanceTieThresholdMeters
                           && item.Location.Priority == first.Location.Priority)
            .ToList();

        var selected = ResolveByQueue(tieCandidates);
        if (selected is null)
        {
            return;
        }

        NearbyLocationDetected?.Invoke(this, new NearbyLocationEventArgs
        {
            LocationId = selected.Location.Id,
            LocationName = selected.Location.Name,
            DistanceMeters = selected.DistanceMeters
        });
    }

    private static double GetEffectiveScanRadiusMeters(double? locationAccuracyMeters)
    {
        var accuracy = locationAccuracyMeters.GetValueOrDefault(MinimumScanRadiusMeters);
        var adjusted = Math.Max(MinimumScanRadiusMeters, accuracy * AccuracyRadiusMultiplier);
        return Math.Min(MaximumScanRadiusMeters, adjusted);
    }

    private NearbyCandidate? ResolveByQueue(IReadOnlyList<NearbyCandidate> tieCandidates)
    {
        if (tieCandidates.Count == 0)
        {
            return null;
        }

        foreach (var queuedLocationId in _nearbyQueue)
        {
            var queued = tieCandidates.FirstOrDefault(item =>
                string.Equals(item.Location.Id, queuedLocationId, StringComparison.OrdinalIgnoreCase));
            if (queued is not null)
            {
                return queued;
            }
        }

        var chosen = tieCandidates[0];
        var chosenId = chosen.Location.Id;
        _nearbyQueue.RemoveAll(id => string.Equals(id, chosenId, StringComparison.OrdinalIgnoreCase));
        _nearbyQueue.Add(chosenId);
        if (_nearbyQueue.Count > 30)
        {
            _nearbyQueue.RemoveRange(0, _nearbyQueue.Count - 30);
        }

        return chosen;
    }

    /// <summary>
    /// Calculate distance in km between two GPS coordinates using Haversine formula.
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        => DistanceCalculator.CalculateDistanceKm(lat1, lon1, lat2, lon2);

    private sealed record NearbyCandidate(Models.Location Location, double DistanceMeters);

    public void Dispose()
    {
        StopTracking();
    }
}
