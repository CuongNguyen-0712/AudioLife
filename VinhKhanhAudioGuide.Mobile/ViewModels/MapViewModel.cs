using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

[QueryProperty(nameof(TourId), "TourId")]
[QueryProperty(nameof(ResumeLocationId), "ResumeLocationId")]
[QueryProperty(nameof(ResumeAudioGuideId), "ResumeAudioGuideId")]
[QueryProperty(nameof(ResumeAudioUrl), "ResumeAudioUrl")]
[QueryProperty(nameof(ResumePositionSeconds), "ResumePositionSeconds")]
[QueryProperty(nameof(ResumeSessionId), "ResumeSessionId")]
public partial class MapViewModel : LoadStateViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IGeolocationService _geolocationService;
    private readonly IApiService _apiService;
    private readonly IAudioService _audioService;
    private readonly ITourCheckpointService _tourCheckpointService;
    private readonly ITourPlaybackSessionService _tourPlaybackSessionService;
    private readonly SemaphoreSlim _loadMapLock = new(1, 1);
    private bool _hasLoadedMapData;
    private bool _isSubscribedToAudioEvents;
    private bool _pendingTourRouteLoad;
    private string _loadedTourId = string.Empty;
    private string _currentTourAudioUrl = string.Empty;

    [ObservableProperty]
    private string _tourId = string.Empty;

    [ObservableProperty]
    private bool _isTourRouteMode;

    [ObservableProperty]
    private bool _isTourPaused;

    [ObservableProperty]
    private string _resumeLocationId = string.Empty;

    [ObservableProperty]
    private string _resumeAudioGuideId = string.Empty;

    [ObservableProperty]
    private string _resumeAudioUrl = string.Empty;

    [ObservableProperty]
    private double _resumePositionSeconds;

    [ObservableProperty]
    private string _resumeSessionId = string.Empty;

    [ObservableProperty]
    private string _sectionTitle = "ĐỊA ĐIỂM GẦN ĐÂY";

    [ObservableProperty]
    private string _sectionHint = "Theo khoảng cách";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private double _userLatitude = 21.0285;

    [ObservableProperty]
    private double _userLongitude = 105.8542;

    [ObservableProperty]
    private HtmlWebViewSource? _mapHtmlSource;

    [ObservableProperty]
    private NearbyLocation? _currentPoiLocation;

    public ObservableCollection<MapMarker> MapMarkers { get; } = new();
    public ObservableCollection<NearbyLocation> NearbyLocations { get; } = new();

    public MapViewModel(
        INavigationService navigationService,
        IGeolocationService geolocationService,
        IApiService apiService,
        IAudioService audioService,
        ITourCheckpointService tourCheckpointService,
        ITourPlaybackSessionService tourPlaybackSessionService)
    {
        _navigationService = navigationService;
        _geolocationService = geolocationService;
        _apiService = apiService;
        _audioService = audioService;
        _tourCheckpointService = tourCheckpointService;
        _tourPlaybackSessionService = tourPlaybackSessionService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query is null || query.Count == 0)
        {
            return;
        }

        if (query.TryGetValue("TourId", out var tourIdValue))
        {
            TourId = tourIdValue?.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("ResumeLocationId", out var resumeLocationValue))
        {
            ResumeLocationId = resumeLocationValue?.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("ResumeAudioGuideId", out var resumeGuideValue))
        {
            ResumeAudioGuideId = resumeGuideValue?.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("ResumeAudioUrl", out var resumeAudioUrlValue))
        {
            ResumeAudioUrl = resumeAudioUrlValue?.ToString() ?? string.Empty;
        }

        if (query.TryGetValue("ResumePositionSeconds", out var resumePositionValue)
            && double.TryParse(resumePositionValue?.ToString(), out var parsedPositionSeconds))
        {
            ResumePositionSeconds = parsedPositionSeconds;
        }

        if (query.TryGetValue("ResumeSessionId", out var resumeSessionValue))
        {
            ResumeSessionId = resumeSessionValue?.ToString() ?? string.Empty;
        }
    }

    partial void OnTourIdChanged(string value)
    {
        IsTourRouteMode = !string.IsNullOrWhiteSpace(value);
        _hasLoadedMapData = false;
        IsTourPaused = false;

        if (IsTourRouteMode)
        {
            _pendingTourRouteLoad = true;
            // Don't call LoadTourRouteAsync here - let OnAppearingAsync handle it
            // This prevents race conditions and ensures data loads before UI appears
        }
    }

    partial void OnIsTourRouteModeChanged(bool value)
    {
        OnPropertyChanged(nameof(TourActionText));
        OnPropertyChanged(nameof(IsTourActionVisible));
    }

    partial void OnIsTourPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(TourActionText));
        OnPropertyChanged(nameof(IsTourActionVisible));
    }

    partial void OnResumeSessionIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        IsTourPaused = false;
        _hasLoadedMapData = false;
        _pendingTourRouteLoad = true;

        // Don't call LoadTourRouteAsync here - let OnAppearingAsync handle it
        // This prevents race conditions and ensures data loads before UI appears
    }

    public async Task OnAppearingAsync()
    {
        if (!_isSubscribedToAudioEvents)
        {
            _audioService.StateChanged += OnAudioStateChanged;
            _isSubscribedToAudioEvents = true;
        }

        // Check actual TourId property (in case [QueryProperty] just applied it)
        var hasTourId = !string.IsNullOrWhiteSpace(TourId);
        var isTourRouteMode = hasTourId;

        if (isTourRouteMode
            && _hasLoadedMapData
            && string.Equals(_loadedTourId, TourId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!isTourRouteMode && _hasLoadedMapData)
        {
            await RefreshMapWithLocationAsync();
            return;
        }

        if (isTourRouteMode || _pendingTourRouteLoad)
        {
            _pendingTourRouteLoad = false;
            await LoadTourRouteAsync();
            return;
        }

        await RefreshMapWithLocationAsync(forceRefresh: true);
    }

    [RelayCommand]
    private async Task TourActionAsync()
    {
        if (!IsTourRouteMode)
        {
            return;
        }

        if (IsTourPaused)
        {
            await ContinueTourAsync();
            return;
        }

        await ShowTourActionSheetAsync();
    }

    public async Task<bool> RequestExitTourAsync()
    {
        if (!IsTourRouteMode)
        {
            return false;
        }

        if (IsTourPaused)
        {
            return false;
        }

        await ShowTourActionSheetAsync();
        return true;
    }

    public string TourActionText => !IsTourRouteMode
        ? string.Empty
        : (IsTourPaused ? "Tiếp tục" : "Tạm dừng");

    public bool IsTourActionVisible => IsTourRouteMode;

    private async Task ShowTourActionSheetAsync()
    {
        var action = await MainThread.InvokeOnMainThreadAsync(async () =>
            await Application.Current!.MainPage!.DisplayActionSheet(
                "Thoát lộ trình",
                "Tiếp tục lộ trình",
                null,
                "Tạm dừng & Lưu",
                "Kết thúc tour"));

        if (string.Equals(action, "Tạm dừng & Lưu", StringComparison.Ordinal))
        {
            await SaveCheckpointAsync();
            await _audioService.StopAsync();
            IsTourPaused = true;
            OnPropertyChanged(nameof(TourActionText));
            OnPropertyChanged(nameof(IsTourActionVisible));
            return;
        }

        if (string.Equals(action, "Kết thúc tour", StringComparison.Ordinal))
        {
            await _tourCheckpointService.ClearAsync();
            await _audioService.StopAsync();
            await ExitTourToNormalMapAsync();
            return;
        }
    }

    private async Task ContinueTourAsync()
    {
        var checkpoint = await _tourCheckpointService.GetAsync();
        if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.TourId))
        {
            IsTourPaused = false;
            OnPropertyChanged(nameof(TourActionText));
            return;
        }

        await ResumeTourAudioAsync(checkpoint.AudioUrl, checkpoint.AudioPositionSeconds);
        await _tourCheckpointService.ClearAsync();
        IsTourPaused = false;
        OnPropertyChanged(nameof(TourActionText));
    }

    public void OnDisappearing()
    {
        if (!_isSubscribedToAudioEvents)
        {
            return;
        }

        _audioService.StateChanged -= OnAudioStateChanged;
        _isSubscribedToAudioEvents = false;
    }

    private async Task ResumeTourAudioAsync(string audioUrl, double positionSeconds)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return;
        }

        await _audioService.PlayAsync(audioUrl);

        if (positionSeconds > 0)
        {
            await _audioService.SeekAsync(TimeSpan.FromSeconds(positionSeconds));
        }
    }

    private async Task PlayCurrentTourPoiAsync()
    {
        if (!IsTourRouteMode || CurrentPoiLocation is null)
        {
            return;
        }

        var (_, audioUrl) = await ResolveTourLocationAudioAsync(CurrentPoiLocation.Id);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            await AdvanceTourLocationAsync();
            return;
        }

        _currentTourAudioUrl = audioUrl;
        await _audioService.PlayAsync(audioUrl);
    }

    private async Task<(string audioGuideId, string audioUrl)> ResolveTourLocationAudioAsync(string locationId)
    {
        var location = await _apiService.GetLocationByIdAsync(locationId);
        if (location == null)
        {
            return (string.Empty, string.Empty);
        }

        var guide = location.AudioGuides.FirstOrDefault();
        if (guide == null)
        {
            return (string.Empty, string.Empty);
        }

        var audioUrl = !string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl)
            ? guide.CloudinaryAudioUrl
            : guide.AudioUrl;

        return (guide.Id, audioUrl);
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        if (!IsTourRouteMode || !_tourPlaybackSessionService.IsActive)
        {
            return;
        }

        if (e.State != AudioPlaybackState.Stopped || string.IsNullOrWhiteSpace(e.AudioUrl))
        {
            return;
        }

        if (!string.Equals(e.AudioUrl, _currentTourAudioUrl, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = MainThread.InvokeOnMainThreadAsync(AdvanceTourLocationAsync);
    }

    private async Task AdvanceTourLocationAsync()
    {
        if (!_tourPlaybackSessionService.TryMoveNextLocation(out var nextLocationId))
        {
            await FinishTourAsync();
            return;
        }

        var (_, audioUrl) = await ResolveTourLocationAudioAsync(nextLocationId);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            await AdvanceTourLocationAsync();
            return;
        }

        _currentTourAudioUrl = audioUrl;
        await _audioService.PlayAsync(audioUrl);
    }

    private async Task FinishTourAsync()
    {
        _tourPlaybackSessionService.Reset();
        _currentTourAudioUrl = string.Empty;

        await MainThread.InvokeOnMainThreadAsync(async () =>
            await Application.Current!.MainPage!.DisplayAlert(
                "Hoàn thành tour",
                "Bạn đã nghe hết các địa điểm trong tour.",
                "Về bản đồ"));

        await ExitTourToNormalMapAsync();
    }

    private async Task ExitTourToNormalMapAsync()
    {
        IsTourRouteMode = false;
        IsTourPaused = false;
        TourId = string.Empty;
        ResumeLocationId = string.Empty;
        ResumeAudioGuideId = string.Empty;
        ResumeAudioUrl = string.Empty;
        ResumePositionSeconds = 0;
        ResumeSessionId = string.Empty;
        _loadedTourId = string.Empty;
        _hasLoadedMapData = false;
        _pendingTourRouteLoad = false;
        _tourPlaybackSessionService.Reset();
        _currentTourAudioUrl = string.Empty;
        OnPropertyChanged(nameof(TourActionText));
        OnPropertyChanged(nameof(IsTourActionVisible));

        await GetUserLocationAsync();
        await LoadMapDataAsync();
    }

    /// <summary>
    /// Get current user location from geolocation service.
    /// </summary>
    private async Task<bool> GetUserLocationAsync()
    {
        try
        {
            var location = await _geolocationService.GetCurrentLocationAsync();
            if (!location.HasValue)
            {
                return false;
            }

            UserLatitude = location.Value.Latitude;
            UserLongitude = location.Value.Longitude;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void LoadMapData()
    {
        _ = LoadMapDataAsync();
    }

    /// <summary>
    /// Refresh map by getting user location and reloading map data (called from MapPage OnAppearing).
    /// </summary>
    public async Task RefreshMapWithLocationAsync(bool forceRefresh = false)
    {
        if (!string.IsNullOrWhiteSpace(TourId))
        {
            await LoadTourRouteAsync();
            return;
        }

        if (_hasLoadedMapData && !forceRefresh)
        {
            return;
        }

        try
        {
            await GetUserLocationAsync();
        }
        catch
        {
            // Keep previous/default coordinates.
        }

        await LoadMapDataAsync();
    }

    private async Task<Tour?> ResolveTourByIdAsync(string tourId)
    {
        var tour = await _apiService.GetTourByIdAsync(tourId);
        if (tour != null)
        {
            return tour;
        }

        var tours = await _apiService.GetToursAsync();
        return tours.ElementAtOrDefault(int.TryParse(tourId, out var idx) ? idx - 1 : -1);
    }

    private async Task LoadTourRouteAsync()
    {
        await _loadMapLock.WaitAsync();
        try
        {
            BeginLoading();
            IsTourPaused = false;
            MapMarkers.Clear();
            NearbyLocations.Clear();

            if (string.IsNullOrWhiteSpace(TourId))
            {
                FailLoading("Không tìm thấy lộ trình.");
                return;
            }

            await GetUserLocationAsync();

            var tour = await ResolveTourByIdAsync(TourId);
            if (tour == null)
            {
                FailLoading("Không tìm thấy lộ trình.");
                return;
            }

            var locationsTask = _apiService.GetLocationsAsync();
            var categoriesTask = _apiService.GetCategoriesAsync();
            await Task.WhenAll(locationsTask, categoriesTask);

            var allLocations = locationsTask.Result;
            var categories = categoriesTask.Result;

            var routeLocations = new List<Models.Location>();
            foreach (var locationId in tour.LocationIds)
            {
                var found = allLocations.FirstOrDefault(l => l.Id == locationId);
                if (found != null)
                {
                    routeLocations.Add(found);
                }
            }

            routeLocations = OptimizeTourLocationsByDistance(routeLocations);

            if (routeLocations.Count == 0)
            {
                FailLoading("Lộ trình chưa có địa điểm.");
                return;
            }

            var locationPoints = routeLocations
                .Select((location, index) => BuildLocationPoint(location, index))
                .ToList();

            _tourPlaybackSessionService.Initialize(
                TourId,
                routeLocations.Select(location => location.Id).ToList(),
                !string.IsNullOrWhiteSpace(ResumeLocationId) ? ResumeLocationId : routeLocations.First().Id);

            for (var i = 0; i < locationPoints.Count; i++)
            {
                var point = locationPoints[i];
                var location = point.Location;
                var category = categories.FirstOrDefault(c => c.Id == location.CategoryId);

                MapMarkers.Add(new MapMarker
                {
                    Id = location.Id,
                    Name = location.Name,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude
                });

                NearbyLocations.Add(new NearbyLocation
                {
                    Id = location.Id,
                    Name = location.Name,
                    ImageUrl = location.ImageUrl,
                    CategoryName = category?.Name ?? "Khác",
                    Address = location.Address,
                    AudioCount = location.AudioGuides?.Count ?? 0,
                    IsNearest = i == 0,
                    IsHot = i == 0,
                    TourOrder = i + 1,
                    MetaText = $"Điểm dừng {i + 1}/{locationPoints.Count}",
                    BadgeText = $"POI {i + 1}",
                    IsBadgeVisible = true
                });
            }

            CurrentPoiLocation = NearbyLocations.FirstOrDefault();
            ApplyResumeCheckpointToCurrentPoi();
            SectionTitle = "ĐIỂM DỪNG LỘ TRÌNH";
            SectionHint = "Theo thứ tự";

            GenerateMapHtml(locationPoints, categories, showRoute: true);

            if (!string.IsNullOrWhiteSpace(ResumeSessionId) && !string.IsNullOrWhiteSpace(ResumeAudioUrl))
            {
                _currentTourAudioUrl = ResumeAudioUrl;
                await ResumeTourAudioAsync(ResumeAudioUrl, ResumePositionSeconds);
                await _tourCheckpointService.ClearAsync();
            }
            else
            {
                await PlayCurrentTourPoiAsync();
            }

            _loadedTourId = TourId;
            _hasLoadedMapData = true;
            CompleteLoading(MapMarkers.Count > 0);
        }
        catch (Exception ex)
        {
            FailLoading(ex.Message);
        }
        finally
        {
            _loadMapLock.Release();
        }
    }

    private void ApplyResumeCheckpointToCurrentPoi()
    {
        if (string.IsNullOrWhiteSpace(ResumeLocationId))
        {
            return;
        }

        var resumed = NearbyLocations.FirstOrDefault(item => string.Equals(item.Id, ResumeLocationId, StringComparison.OrdinalIgnoreCase));
        if (resumed == null)
        {
            return;
        }

        foreach (var item in NearbyLocations)
        {
            item.IsNearest = false;
        }

        resumed.IsNearest = true;
        resumed.BadgeText = "Đang tiếp tục";
        resumed.IsBadgeVisible = true;
        resumed.MetaText = $"Tiếp tục từ điểm dừng {resumed.TourOrder}/{NearbyLocations.Count}";
        CurrentPoiLocation = resumed;
    }

    private async Task SaveCheckpointAsync()
    {
        if (!IsTourRouteMode || string.IsNullOrWhiteSpace(TourId))
        {
            return;
        }

        var locationId = CurrentPoiLocation?.Id ?? string.Empty;
        var locationName = CurrentPoiLocation?.Name ?? "điểm dừng hiện tại";
        var audioUrl = _audioService.CurrentAudioUrl ?? string.Empty;
        var audioGuideId = string.Empty;

        if (!string.IsNullOrWhiteSpace(locationId) && !string.IsNullOrWhiteSpace(audioUrl))
        {
            audioGuideId = await ResolveAudioGuideIdAsync(locationId, audioUrl);
        }

        var checkpoint = new TourCheckpoint
        {
            TourId = TourId,
            LocationId = locationId,
            LocationName = locationName,
            AudioGuideId = audioGuideId,
            AudioUrl = audioUrl,
            AudioPositionSeconds = Math.Max(0, _audioService.CurrentPosition.TotalSeconds),
            SavedAtUtc = DateTime.UtcNow
        };

        await _tourCheckpointService.SaveAsync(checkpoint);
    }

    private async Task<string> ResolveAudioGuideIdAsync(string locationId, string audioUrl)
    {
        var location = await _apiService.GetLocationByIdAsync(locationId);
        if (location == null)
        {
            return string.Empty;
        }

        var guide = location.AudioGuides.FirstOrDefault(item =>
            string.Equals(item.AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.CloudinaryAudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase));

        return guide?.Id ?? string.Empty;
    }

    public async Task LoadMapDataAsync()
    {
        await _loadMapLock.WaitAsync();
        try
        {
            BeginLoading();
            MapMarkers.Clear();
            NearbyLocations.Clear();

            var locationsTask = _apiService.GetLocationsAsync();
            var categoriesTask = _apiService.GetCategoriesAsync();
            var featuredToursTask = _apiService.GetFeaturedToursAsync();
            await Task.WhenAll(locationsTask, categoriesTask, featuredToursTask);

            var locations = locationsTask.Result;
            var categories = categoriesTask.Result;
            var featuredTours = featuredToursTask.Result;
            var featuredLocationIds = featuredTours
                .SelectMany(t => t.LocationIds)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var locationPoints = locations
                .Select((location, index) => BuildLocationPoint(location, index))
                .ToList();

            foreach (var point in locationPoints)
            {
                var location = point.Location;
                MapMarkers.Add(new MapMarker
                {
                    Id = location.Id,
                    Name = location.Name,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude
                });

                var distanceMeters = CalculateDistance(UserLatitude, UserLongitude, point.Latitude, point.Longitude) * 1000;
                var category = categories.FirstOrDefault(c => c.Id == location.CategoryId);
                NearbyLocations.Add(new NearbyLocation
                {
                    Id = location.Id,
                    Name = location.Name,
                    ImageUrl = location.ImageUrl,
                    CategoryName = category?.Name ?? "Khác",
                    Address = location.Address,
                    Distance = Math.Round(distanceMeters),
                    AudioCount = location.AudioGuides?.Count ?? 0,
                    IsHot = featuredLocationIds.Contains(location.Id),
                    MetaText = $"Cách bạn {DistanceFormatService.FormatDistance(distanceMeters)}"
                });
            }

            // Sort by distance and keep all POIs in the list.
            var sorted = NearbyLocations.OrderBy(x => x.Distance).ToList();
            NearbyLocations.Clear();
            for (var i = 0; i < sorted.Count; i++)
            {
                var loc = sorted[i];
                loc.IsNearest = i == 0;
                loc.IsBadgeVisible = i == 0;
                loc.BadgeText = i == 0 ? "Gần nhất" : string.Empty;
                NearbyLocations.Add(loc);
            }

            CurrentPoiLocation = NearbyLocations.FirstOrDefault(x => x.IsNearest) ?? NearbyLocations.FirstOrDefault();
            SectionTitle = "ĐỊA ĐIỂM GẦN ĐÂY";
            SectionHint = "Theo khoảng cách";

            // Generate Leaflet map HTML
            GenerateMapHtml(locationPoints, categories, showRoute: false);
            _hasLoadedMapData = true;
            _loadedTourId = string.Empty;
            CompleteLoading(MapMarkers.Count > 0);
        }
        catch (Exception ex)
        {
            FailLoading(ex.Message);
        }
        finally
        {
            _loadMapLock.Release();
        }
    }

    private void GenerateMapHtml(List<LocationPoint> locations, List<Category> categories, bool showRoute)
    {
        var primary = GetThemeColorHex("Primary", "#13696D");
        var onPrimary = GetThemeColorHex("OnPrimary", "#FFFFFF");
        var secondary = GetThemeColorHex("Secondary", "#456466");
        var secondaryContainer = GetThemeColorHex("SecondaryContainer", "#C5E6E8");
        var onSecondaryContainer = GetThemeColorHex("OnSecondaryContainer", "#49686A");
        var tertiary = GetThemeColorHex("Tertiary", "#8A4F30");
        var tertiaryFixed = GetThemeColorHex("TertiaryFixed", "#FFDBCB");
        var onTertiaryFixed = GetThemeColorHex("OnTertiaryFixed", "#341100");
        var surfaceContainerLowest = GetThemeColorHex("SurfaceContainerLowest", "#FFFFFF");
        var onSurface = GetThemeColorHex("OnSurface", "#191C1B");
        var onSurfaceVariant = GetThemeColorHex("OnSurfaceVariant", "#3F4949");
        var primarySweep = HexToRgba(primary, 0.34);
        var primarySweepFade = HexToRgba(primary, 0.03);

        var nearestLocationId = NearbyLocations.FirstOrDefault(x => x.IsNearest)?.Id;
        var orderByLocationId = NearbyLocations
            .Where(item => item.TourOrder > 0 && !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(item => item.Id, item => item.TourOrder, StringComparer.OrdinalIgnoreCase);
        var markersJs = new StringBuilder();
        const string audioMetaIcon = "audio.svg";
        const string timeMetaIcon = "time.svg";
        const string showMoreIcon = "show_more.svg";
        foreach (var point in locations)
        {
            var loc = point.Location;
            var cat = categories.FirstOrDefault(c => c.Id == loc.CategoryId);
            var escapedName = loc.Name.Replace("'", "\\'");
            var escapedAddr = loc.Address.Replace("'", "\\'");
            var escapedCat = (cat?.Name ?? "Khác").Replace("'", "\\'");
            var escapedId = Uri.EscapeDataString(loc.Id);
            var escapedImage = Uri.EscapeDataString(loc.ImageUrl);
            var audioCount = loc.AudioGuides?.Count ?? 0;
            var isNearest = string.Equals(nearestLocationId, loc.Id, StringComparison.Ordinal);
            var order = orderByLocationId.TryGetValue(loc.Id, out var tourOrder) ? tourOrder : 0;
            var markerIcon = showRoute && order > 0
                ? "L.divIcon({className:'tour-route-marker',html:'<div class=\"tour-poi-wrapper\"><img src=\"location_icon.svg\" class=\"tour-poi-pin\"/><div class=\"tour-poi-order\">" + order + "</div></div>',iconSize:[48,56],iconAnchor:[24,52],popupAnchor:[0,-48]})"
                : (isNearest ? "nearestIcon" : "customIcon");
            var nearestTag = isNearest
                ? $"<br/><span style=\"display:inline-block;margin-top:4px;padding:2px 8px;border-radius:999px;background:{tertiaryFixed};color:{onTertiaryFixed};font-size:11px;font-weight:700;\">Gần nhất</span>"
                : string.Empty;
            markersJs.AppendLine(
                $"L.marker([{point.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{point.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{icon: {markerIcon}}})" +
                $".addTo(map).bindPopup('<div style=\"display:flex;align-items:center;gap:8px;min-width:236px;max-width:258px;font-family:RobotoCondensed-Regular,-apple-system,Segoe UI,sans-serif;color:{onSurface};line-height:1.25;\">" +
                $"<div style=\"flex:0 0 64px;width:64px;height:64px;border-radius:14px;overflow:hidden;background:{surfaceContainerLowest};\"><img src=\"{escapedImage}\" style=\"width:64px;height:64px;object-fit:cover;display:block;\" /></div>" +
                $"<div style=\"flex:1;min-width:0;display:flex;flex-direction:column;justify-content:center;\">" +
                $"<div style=\"font-family:RobotoCondensed-SemiBold,-apple-system,Segoe UI,sans-serif;font-size:13px;line-height:1.15;color:{onSurface};white-space:normal;\">{escapedName}</div>{nearestTag}<div style=\"margin-top:2px;font-size:10px;color:{onSurfaceVariant};\">{escapedCat}</div>" +
                $"<div style=\"display:flex;align-items:center;gap:4px;margin-top:4px;font-size:10px;color:{onSurfaceVariant};\"><img src=\"{audioMetaIcon}\" style=\"width:12px;height:12px;flex:0 0 12px;opacity:0.95;\"/> <span>{audioCount} audio</span></div>" +
                $"<div style=\"display:flex;align-items:center;gap:4px;margin-top:2px;font-size:10px;color:{onSurfaceVariant};\"><img src=\"{timeMetaIcon}\" style=\"width:12px;height:12px;flex:0 0 12px;opacity:0.95;\"/> <span>{loc.Duration} phút</span></div></div>" +
                $"<div style=\"flex:0 0 30px;display:flex;align-items:center;justify-content:center;align-self:center;\"><a href=\"app://poi/{escapedId}\" style=\"width:30px;height:30px;display:inline-flex;align-items:center;justify-content:center;border-radius:999px;background:{secondaryContainer};color:{onSecondaryContainer};text-decoration:none;flex:0 0 30px;\"><img src=\"{showMoreIcon}\" style=\"width:16px;height:16px;display:block;\" /></a></div>" +
                $"</div></div>');");
        }

        var routeJs = string.Empty;
        if (showRoute && locations.Count > 1)
        {
            var routeCoords = string.Join(",", locations.Select(point =>
                $"[{point.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{point.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}]"));
            routeJs = $@"
    var routeLine = L.polyline([{routeCoords}], {{
        color:'#13696D',
        weight:4,
        opacity:0.85,
        lineJoin:'round'
    }}).addTo(map);

    var bounds = routeLine.getBounds();
    if (bounds && bounds.isValid()) {{
        map.fitBounds(bounds.pad(0.2));
    }}";
        }

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'/>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body {{ margin:0; padding:0; }}
        #map {{ width:100%; height:100vh; }}
        .custom-popup .leaflet-popup-content {{ font-family: -apple-system, sans-serif; font-size:13px; }}
        .custom-zoom {{
            position: absolute;
            left: 16px;
            top: 16px;
            z-index: 1000;
            display: flex;
            flex-direction: column;
            gap: 8px;
        }}
        .custom-zoom button {{
            width: 40px;
            height: 40px;
            border: 0;
            border-radius: 12px;
            background: {secondaryContainer};
            opacity: 0.8;
            color: {onSecondaryContainer};
            font-size: 24px;
            line-height: 1;
            font-weight: 700;
            box-shadow: 0 4px 12px rgba(0,0,0,0.16);
            cursor: pointer;
        }}
        .user-radar-marker {{
            position: relative;
            width: 88px;
            height: 88px;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: visible;
        }}
        .user-sweep {{
            position: absolute;
            width: 100px;
            height: 100px;
            border-radius: 50%;
            background: conic-gradient(from 0deg, {primarySweep} 0deg, {primarySweepFade} 140deg, transparent 240deg, transparent 360deg);
            animation: radarSweep 2.2s linear infinite;
            opacity: 0.85;
        }}
        .user-core {{
            width: 16px;
            height: 16px;
            border-radius: 50%;
            background: {secondary};
            border: 3px solid {surfaceContainerLowest};
            box-shadow: 0 2px 6px rgba(0,0,0,0.22);
            z-index: 3;
        }}
        .tour-poi-wrapper {{
            position:relative;
            width:48px;
            height:56px;
            display:flex;
            align-items:flex-start;
            justify-content:center;
        }}
        .tour-poi-pin {{
            width:40px;
            height:40px;
            display:block;
            filter:drop-shadow(0 3px 8px rgba(0,0,0,0.28));
        }}
        .tour-poi-order {{
            position:absolute;
            right:-2px;
            top:-2px;
            width:18px;
            height:18px;
            border-radius:9px;
            background:#8A4F30;
            color:#FFFFFF;
            border:2px solid #FFFFFF;
            display:flex;
            align-items:center;
            justify-content:center;
            font-family:RobotoCondensed-SemiBold,-apple-system,Segoe UI,sans-serif;
            font-size:10px;
            line-height:1;
            box-shadow:0 2px 6px rgba(0,0,0,0.26);
        }}
        @keyframes radarSweep {{
            from {{ transform: rotate(0deg); }}
            to {{ transform: rotate(360deg); }}
        }}
    </style>
</head>
<body>
<div id='map'></div>
<div class='custom-zoom'>
    <button id='zoomInBtn' type='button'>+</button>
    <button id='zoomOutBtn' type='button'>-</button>
</div>
<script>
    var map = L.map('map', {{ zoomControl: false }}).setView([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 13);
    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }}).addTo(map);

    map.on('popupopen', function(e) {{
        if (!e.popup || !e.popup.getElement()) return;
        var popupEl = e.popup.getElement();
        var wrapper = popupEl.querySelector('.leaflet-popup-content-wrapper');
        if (wrapper) {{
            wrapper.style.background = 'rgba(255,255,255,0.78)';
            wrapper.style.color = '{onSurface}';
            wrapper.style.borderRadius = '16px';
            wrapper.style.boxShadow = '0 8px 18px rgba(0,0,0,0.16)';
            wrapper.style.backdropFilter = 'blur(10px)';
            wrapper.style.webkitBackdropFilter = 'blur(10px)';
        }}
        var tip = popupEl.querySelector('.leaflet-popup-tip');
        if (tip) {{
            tip.style.background = 'rgba(255,255,255,0.78)';
        }}
    }});

    var customIcon = L.divIcon({{
        className: 'custom-marker',
        html: '<img src=""location_icon.svg"" style=""width:40px;height:40px;display:block;filter:drop-shadow(0 3px 8px rgba(0,0,0,0.3));""/>',
        iconSize: [48, 48],
        iconAnchor: [24, 48],
        popupAnchor: [0, -42]
    }});

    var nearestIcon = L.divIcon({{
        className: 'nearest-marker',
        html: '<div style=""width:48px;height:48px;border-radius:24px;background:#8A4F30;display:flex;align-items:center;justify-content:center;box-shadow:0 6px 14px rgba(0,0,0,0.35);""><img src=""location_icon.svg"" style=""width:30px;height:30px;display:block;filter:brightness(0) invert(1);""/></div>',
        iconSize: [48, 48],
        iconAnchor: [24, 48],
        popupAnchor: [0, -42]
    }});

    // User location marker
    var userIcon = L.divIcon({{
        className: 'user-marker',
        html: '<div class=""user-radar-marker""><span class=""user-sweep""></span><span class=""user-core""></span></div>',
        iconSize: [88, 88],
        iconAnchor: [44, 44]
    }});
    L.marker([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{icon: userIcon}}).addTo(map).bindPopup('📍 Vị trí của bạn');

    document.getElementById('zoomInBtn').addEventListener('click', function() {{ map.zoomIn(); }});
    document.getElementById('zoomOutBtn').addEventListener('click', function() {{ map.zoomOut(); }});

    // Location markers
    {markersJs}
    {routeJs}
</script>
</body>
</html>";

        MapHtmlSource = new HtmlWebViewSource
        {
            Html = html,
            BaseUrl = GetWebViewBaseUrl()
        };
    }

    [RelayCommand]
    private async Task MoveToCurrentLocationAsync()
    {
        var location = await _geolocationService.GetCurrentLocationAsync();
        if (location is null)
        {
            return;
        }

        UserLatitude = location.Value.Latitude;
        UserLongitude = location.Value.Longitude;
        LoadMapData();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    [RelayCommand]
    private async Task LocationTappedAsync(NearbyLocation? location)
    {
        if (location is null) return;

        CurrentPoiLocation = location;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", location.Id } });
    }

    public async Task OpenPoiDetailByIdFromMapAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return;
        }

        var selected = NearbyLocations.FirstOrDefault(x => string.Equals(x.Id, locationId, StringComparison.Ordinal));
        if (selected is not null)
        {
            CurrentPoiLocation = selected;
        }

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", locationId } });
    }

    private static string GetThemeColorHex(string resourceKey, string fallback)
    {
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true)
        {
            if (resource is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var valueText = resource?.ToString();
            if (!string.IsNullOrWhiteSpace(valueText) && valueText.StartsWith("#", StringComparison.Ordinal))
            {
                return valueText;
            }

            var toArgbHex = resource?.GetType().GetMethod("ToArgbHex", Type.EmptyTypes);
            if (toArgbHex is not null)
            {
                var hex = toArgbHex.Invoke(resource, null)?.ToString();
                if (!string.IsNullOrWhiteSpace(hex) && hex.StartsWith("#", StringComparison.Ordinal))
                {
                    return hex;
                }
            }
        }

        return fallback;
    }

    private static string HexToRgba(string hex, double alpha)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return $"rgba(19,105,109,{alpha.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})";
        }

        var value = hex.Trim().TrimStart('#');
        if (value.Length == 8)
        {
            value = value[2..];
        }

        if (value.Length != 6)
        {
            return $"rgba(19,105,109,{alpha.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})";
        }

        var r = Convert.ToInt32(value[..2], 16);
        var g = Convert.ToInt32(value.Substring(2, 2), 16);
        var b = Convert.ToInt32(value.Substring(4, 2), 16);
        var a = Math.Clamp(alpha, 0, 1).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        return $"rgba({r},{g},{b},{a})";
    }

    private static string GetWebViewBaseUrl()
    {
#if ANDROID
        return "file:///android_asset/";
#elif WINDOWS
        return "ms-appx-web:///";
#else
        return string.Empty;
#endif
    }

    private LocationPoint BuildLocationPoint(Models.Location location, int index)
    {
        if (HasValidCoordinate(location.Latitude, location.Longitude))
        {
            return new LocationPoint(location, location.Latitude, location.Longitude);
        }

        var (lat, lng) = GetFallbackCoordinate(index, UserLatitude, UserLongitude);
        return new LocationPoint(location, lat, lng);
    }

    private static bool HasValidCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return false;
        }

        // Default 0,0 indicates missing coordinate data in sample content.
        return Math.Abs(latitude) > 0.000001 || Math.Abs(longitude) > 0.000001;
    }

    private static (double Latitude, double Longitude) GetFallbackCoordinate(int index, double centerLat, double centerLng)
    {
        var slot = index % 8;
        var ring = (index / 8) + 1;
        var angle = slot * (Math.PI / 4);
        var radius = 0.0018 * ring;

        var lat = centerLat + (Math.Sin(angle) * radius);
        var lng = centerLng + (Math.Cos(angle) * radius);
        return (lat, lng);
    }

    private static List<Models.Location> OptimizeTourLocationsByDistance(List<Models.Location> locations)
    {
        if (locations.Count <= 2)
        {
            return locations;
        }

        var valid = new List<Models.Location>();
        var invalid = new List<Models.Location>();
        foreach (var location in locations)
        {
            if (HasValidCoordinate(location.Latitude, location.Longitude))
            {
                valid.Add(location);
            }
            else
            {
                invalid.Add(location);
            }
        }

        if (valid.Count <= 2)
        {
            valid.AddRange(invalid);
            return valid;
        }

        var ordered = new List<Models.Location>();
        var remaining = new List<Models.Location>(valid);
        ordered.Add(remaining[0]);
        remaining.RemoveAt(0);

        while (remaining.Count > 0)
        {
            var current = ordered[^1];
            var nearest = remaining
                .OrderBy(candidate => CalculateDistance(current.Latitude, current.Longitude, candidate.Latitude, candidate.Longitude))
                .First();

            ordered.Add(nearest);
            remaining.Remove(nearest);
        }

        ordered.AddRange(invalid);
        return ordered;
    }
}

public sealed class LocationPoint
{
    public LocationPoint(Models.Location location, double latitude, double longitude)
    {
        Location = location;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Models.Location Location { get; }
    public double Latitude { get; }
    public double Longitude { get; }
}

public class MapMarker
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class NearbyLocation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Distance { get; set; }
    public int AudioCount { get; set; }
    public bool IsHot { get; set; }
    public bool IsNearest { get; set; }
    public int TourOrder { get; set; }
    public string MetaText { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public bool IsBadgeVisible { get; set; }
}
