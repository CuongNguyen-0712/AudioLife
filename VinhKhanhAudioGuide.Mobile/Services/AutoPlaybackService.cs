using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using VinhKhanhAudioGuide.Mobile.Models;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class AutoPlaybackService : IAutoPlaybackService, IDisposable
{
    private const double TriggerRadiusMeters = 5.0;     // TH1: 5m to trigger
    private const double ExitRadiusMeters = 20.0;       // TH1: 20m to consider "left"
    private const double CooldownMinutes = 5.0;         // TH1: 5 min cooldown
    
    private readonly IGeolocationService _geolocationService;
    private readonly IAudioService _audioService;
    private readonly IApiService _apiService;
    private readonly ILocalDatabaseService _localDb;
    
    private readonly HashSet<string> _currentlyInsideLocations = new();
    
    private Location? _previousUserLocation;
    private bool _isActive;
    private string? _interruptedLocationId;
    private TimeSpan _interruptedPosition = TimeSpan.Zero;


    public bool IsActive => _isActive;

    public AutoPlaybackService(
        IGeolocationService geolocationService,
        IAudioService audioService,
        IApiService apiService,
        ILocalDatabaseService localDb)
    {
        _geolocationService = geolocationService;
        _audioService = audioService;
        _apiService = apiService;
        _localDb = localDb;
    }

    public void Start()
    {
        if (_isActive) return;
        _isActive = true;
        _geolocationService.NearbyLocationDetected += OnNearbyLocationDetected;
        _audioService.StateChanged += OnAudioStateChanged;
    }

    public void Stop()
    {
        _isActive = false;
        _geolocationService.NearbyLocationDetected -= OnNearbyLocationDetected;
        _audioService.StateChanged -= OnAudioStateChanged;
    }

    private async void OnNearbyLocationDetected(object? sender, NearbyLocationEventArgs e)
    {
        if (!_isActive || e.Candidates.Count == 0) return;

        var currentUserLocation = _geolocationService.LatestLocation;
        if (currentUserLocation == null) return;

        // TH2: If 2 quán cách đều nhau thì xem khách đang đi về hướng nào
        var candidatesInRange = e.Candidates.Where(c => c.DistanceMeters <= TriggerRadiusMeters).ToList();
        if (candidatesInRange.Count > 0)
        {
            var selected = ResolveTieBreaker(candidatesInRange);
            if (selected != null)
            {
                if (!_currentlyInsideLocations.Contains(selected.LocationId))
                {
                    _currentlyInsideLocations.Add(selected.LocationId);
                    await HandleProximityTriggerAsync(selected.LocationId);
                }
            }
        }

        // Cleanup: Nếu đi ra ngoài 20m thì xóa khỏi danh sách đang ở trong
        var locationsToExit = _currentlyInsideLocations.ToList();
        foreach (var locId in locationsToExit)
        {
            var candidate = e.Candidates.FirstOrDefault(c => c.LocationId == locId);
            if (candidate == null || candidate.DistanceMeters >= ExitRadiusMeters)
            {
                _currentlyInsideLocations.Remove(locId);
            }
        }

        _previousUserLocation = currentUserLocation;
    }

    private NearbyLocationCandidate? ResolveTieBreaker(List<NearbyLocationCandidate> candidates)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // Scoring System to select the best POI to play
        // Weights: Distance (40%), Approach (30%), Priority (20%), History (10%)
        
        var userLoc = _geolocationService.LatestLocation;
        var prevLoc = _previousUserLocation;

        var scores = candidates.Select(c =>
        {
            double totalScore = 0;

            // 1. Distance Score (0-40)
            // Closer is better. 0m = 40 pts, TriggerRadius (5m) = 0 pts.
            double distanceFactor = Math.Max(0, 1 - (c.DistanceMeters / TriggerRadiusMeters));
            totalScore += distanceFactor * 40;

            // 2. Approach Score (0-30)
            // If user is moving towards the POI, give 30 pts.
            if (userLoc != null && prevLoc != null)
            {
                double distNew = DistanceCalculator.CalculateDistanceKm(userLoc.Latitude, userLoc.Longitude, c.Latitude, c.Longitude);
                double distOld = DistanceCalculator.CalculateDistanceKm(prevLoc.Latitude, prevLoc.Longitude, c.Latitude, c.Longitude);
                if (distNew < distOld)
                {
                    totalScore += 30;
                }
            }

            // 3. Priority Score (0-20)
            // Use location priority (0-100). Higher is better.
            double priorityFactor = Math.Clamp(c.Priority / 100.0, 0, 1);
            totalScore += priorityFactor * 20;

            // 4. History Score (0-10)
            // If never played, give 10 pts.
            // Note: In a real scenario, we'd fetch this from DB, but for tie-breaker 
            // we can use a simpler approach or a quick DB check if needed.
            // For now, let's focus on the Heading/Velocity.

            // 5. Heading/Velocity Score (0-30 extra)
            if (userLoc != null && userLoc.Heading.HasValue && userLoc.VelocityMetersPerSecond.HasValue)
            {
                 // Calculate angle to POI
                 var angleToPoi = CalculateAngle(userLoc.Latitude, userLoc.Longitude, c.Latitude, c.Longitude);
                 var diff = Math.Abs((userLoc.Heading.Value - angleToPoi + 360) % 360);
                 if (diff < 45) // Within 45 degrees of heading
                 {
                     totalScore += 20 * (userLoc.VelocityMetersPerSecond.Value / 5.0); // Weighted by speed (max 20 @ 5m/s)
                 }
            }

            return new { Candidate = c, Score = totalScore };
        }).ToList();

        // Sort by score descending and take the best one
        return scores.OrderByDescending(s => s.Score).First().Candidate;
    }

    private async Task HandleProximityTriggerAsync(string locationId)
    {
        // TH1: Check cooldown 5 mins from SQLite
        var lastTime = await _localDb.GetLastPlayedAtAsync(locationId);
        if (lastTime.HasValue)
        {
            var diff = DateTime.UtcNow - lastTime.Value;
            if (diff.TotalMinutes < CooldownMinutes)
            {
                // "bạn đã nghe rồi, có muốn nghe lại không?"
                var result = await MainThread.InvokeOnMainThreadAsync(() => 
                    Shell.Current.DisplayAlert("Thông báo", "Bạn đã nghe thuyết minh này. Bạn có muốn nghe lại không?", "Có", "Không"));
                
                if (!result) return;
            }
        }

        await QueueOrPlayAsync(locationId);
    }

    private async Task QueueOrPlayAsync(string locationId)
    {
        // TH3: Đang nghe quán A, đi ngang quán B -> Không ngắt quán A. Quán B xếp hàng chờ.
        if (_audioService.IsPlaying)
        {
            if (!await _localDb.IsInPlaybackQueueAsync(locationId))
            {
                await _localDb.EnqueuePlaybackAsync(locationId);
            }
            return;
        }

        await PlayLocationAudioAsync(locationId);
    }

    private async Task PlayLocationAudioAsync(string locationId)
    {
        var location = await _apiService.GetLocationByIdAsync(locationId);
        if (location == null || location.AudioGuides.Count == 0) return;

        var guide = location.AudioGuides[0];
        var url = !string.IsNullOrEmpty(guide.CloudinaryAudioUrl) ? guide.CloudinaryAudioUrl : guide.AudioUrl;
        
        if (string.IsNullOrEmpty(url)) return;

        await _localDb.SetLastPlayedAtAsync(locationId, DateTime.UtcNow);
        await _audioService.PlayAsync(url, locationId, guide.Id, isDirectTap: false);
    }

    private async void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        if (e.State == AudioPlaybackState.Stopped || e.State == AudioPlaybackState.None)
        {
            // TH4: Sau khi quán B xong thì hỏi khách: muốn nghe tiếp chỗ bị ngắt của quán A...
            if (_interruptedLocationId != null)
            {
                await HandleResumptionLogicAsync();
                return;
            }

            // TH3: Đợi quán A phát xong thì tự động phát tiếp quán B từ SQLite
            var nextLocationId = await _localDb.DequeuePlaybackAsync();
            if (nextLocationId != null)
            {
                await PlayLocationAudioAsync(nextLocationId);
            }
        }
    }

    public async Task HandleManualPlaybackAsync(string locationId, string audioGuideId)
    {
        // TH4: Đang nghe quán A, bấm tay vào quán B -> Ngắt quán A ngay lập tức, phát quán B liền.
        if (_audioService.IsPlaying)
        {
            _interruptedLocationId = _audioService.CurrentLocationId;
            _interruptedPosition = _audioService.CurrentPosition;
            
            // Record interruption
            if (_interruptedLocationId != null && _audioService.CurrentAudioGuideId != null)
            {
                await _apiService.AddListeningHistoryAsync(
                    _audioService.CurrentAudioGuideId, 
                    _interruptedLocationId, 
                    _audioService.Duration.TotalSeconds > 0 ? _audioService.CurrentPosition.TotalSeconds / _audioService.Duration.TotalSeconds : 0,
                    (int)_interruptedPosition.TotalSeconds,
                    _audioService.IsDirectTap);
            }

            await _audioService.StopAsync();
        }

        // Play B liền
        var location = await _apiService.GetLocationByIdAsync(locationId);
        var guide = location?.AudioGuides.FirstOrDefault(g => g.Id == audioGuideId);
        if (guide != null)
        {
            var url = !string.IsNullOrEmpty(guide.CloudinaryAudioUrl) ? guide.CloudinaryAudioUrl : guide.AudioUrl;
            if (!string.IsNullOrEmpty(url))
            {
                await _audioService.PlayAsync(url, locationId, audioGuideId, isDirectTap: true);
            }
        }
    }

    private async Task HandleResumptionLogicAsync()
    {
        var locId = _interruptedLocationId;
        _interruptedLocationId = null; // Clear state

        if (string.IsNullOrEmpty(locId)) return;

        var action = await MainThread.InvokeOnMainThreadAsync(() => 
            Shell.Current.DisplayActionSheet("Bạn muốn nghe tiếp nội dung bị ngắt?", "Bỏ qua", null, 
                "Nghe tiếp từ chỗ bị ngắt", "Nghe lại từ đầu"));

        if (action == "Nghe tiếp từ chỗ bị ngắt")
        {
            var location = await _apiService.GetLocationByIdAsync(locId);
            if (location?.AudioGuides.Count > 0)
            {
                var guide = location.AudioGuides[0];
                var url = !string.IsNullOrEmpty(guide.CloudinaryAudioUrl) ? guide.CloudinaryAudioUrl : guide.AudioUrl;
                if (!string.IsNullOrEmpty(url))
                {
                    await _audioService.PlayAsync(url, locId, guide.Id);
                    await _audioService.SeekAsync(_interruptedPosition);
                }
            }
        }
        else if (action == "Nghe lại từ đầu")
        {
            await PlayLocationAudioAsync(locId);
        }
        // "Bỏ qua" does nothing
    }

    private static double CalculateAngle(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;

        var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);
        var brng = Math.Atan2(y, x) * 180 / Math.PI;
        return (brng + 360) % 360;
    }

    public void Dispose()
    {
        Stop();
    }
}
