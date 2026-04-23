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
    
    private readonly ConcurrentDictionary<string, DateTime> _lastPlayedAt = new();
    private readonly HashSet<string> _currentlyInsideLocations = new();
    private readonly Queue<string> _pendingPlaybackQueue = new();
    
    private Microsoft.Maui.Devices.Sensors.Location? _previousUserLocation;
    private bool _isActive;
    private string? _interruptedLocationId;
    private TimeSpan _interruptedPosition = TimeSpan.Zero;
    private bool _isProcessingQueue;

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

        // TH2: Xem khách đang đi về hướng nào, quán nào khách đang tiến đến thì phát trước
        var userLoc = _geolocationService.LatestLocation;
        var prevLoc = _previousUserLocation;

        if (userLoc == null || prevLoc == null) return candidates[0];

        var approachData = candidates.Select(c => new
        {
            Candidate = c,
            DistNew = DistanceCalculator.CalculateDistanceKm(userLoc.Latitude, userLoc.Longitude, c.Latitude, c.Longitude),
            DistOld = DistanceCalculator.CalculateDistanceKm(prevLoc.Latitude, prevLoc.Longitude, c.Latitude, c.Longitude)
        }).ToList();

        var approaching = approachData.Where(x => x.DistNew < x.DistOld).OrderBy(x => x.DistNew).ToList();
        
        return approaching.FirstOrDefault()?.Candidate ?? candidates[0];
    }

    private async Task HandleProximityTriggerAsync(string locationId)
    {
        // TH1: Check cooldown 5 mins
        if (_lastPlayedAt.TryGetValue(locationId, out var lastTime))
        {
            var diff = DateTime.UtcNow - lastTime;
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
            if (!_pendingPlaybackQueue.Contains(locationId))
            {
                _pendingPlaybackQueue.Enqueue(locationId);
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

        _lastPlayedAt[locationId] = DateTime.UtcNow;
        await _audioService.PlayAsync(url, locationId, guide.Id);
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

            // TH3: Đợi quán A phát xong thì tự động phát tiếp quán B
            if (_pendingPlaybackQueue.Count > 0)
            {
                var nextLocationId = _pendingPlaybackQueue.Dequeue();
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
                await _audioService.PlayAsync(url, locationId, audioGuideId);
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

    public void Dispose()
    {
        Stop();
    }
}
