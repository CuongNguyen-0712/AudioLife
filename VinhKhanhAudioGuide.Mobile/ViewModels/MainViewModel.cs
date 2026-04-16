using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VinhKhanhAudioGuide.Mobile.Messages;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;
using MainThread = Microsoft.Maui.ApplicationModel.MainThread;
using Preferences = Microsoft.Maui.Storage.Preferences;
using ObservablePropertyAttribute = CommunityToolkit.Mvvm.ComponentModel.ObservablePropertyAttribute;
using RelayCommandAttribute = CommunityToolkit.Mvvm.Input.RelayCommandAttribute;
using WeakReferenceMessenger = CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string PreferredAudioGuideKeyPrefix = "AutoNearestPreferredAudioGuide:";
    private const string PreferredAudioUrlKeyPrefix = "AutoNearestPreferredAudioUrl:";
    private const double DefaultUserScanRadiusMeters = 50;
    private const double GeoEventDebounceSeconds = 2.5; // Debounce interval for geofence event processing

    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;
    private readonly IAudioService _audioService;
    private readonly IGeolocationService _geolocationService;
    private readonly ILocalizationService _localizationService;
    private readonly SemaphoreSlim _autoSwitchLock = new(1, 1);
    private readonly List<AutoQueueItem> _autoQueue = new();
    private bool _hasInitializedAutoAudio;
    private bool _isGeoTrackingSubscribed;
    private double _currentPoiDistanceMeters = double.MaxValue;
    private int _autoQueueIndex = -1;
    private DateTime _lastGeoEventProcessedUtc = DateTime.MinValue; // Debounce: track last geofence event

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Location? _selectedLocation;

    // Hero stats (matching web hero-stats)
    [ObservableProperty]
    private int _locationCount;

    [ObservableProperty]
    private int _audioGuideCount;

    [ObservableProperty]
    private int _tourCount;

    [ObservableProperty]
    private int _categoryCount;

    [ObservableProperty]
    private string _heroImageUrl = "hero_image.jpg";

    [ObservableProperty]
    private string _autoLocationId = string.Empty;

    [ObservableProperty]
    private string _autoLocationName = string.Empty;

    [ObservableProperty]
    private string _autoAudioGuideId = string.Empty;

    [ObservableProperty]
    private string _autoAudioUrl = string.Empty;

    [ObservableProperty]
    private string _footerStatusText = string.Empty;

    [ObservableProperty]
    private string _footerHintText = string.Empty;

    [ObservableProperty]
    private string _footerActionText = string.Empty;

    [ObservableProperty]
    private bool _isFooterActionEnabled;

    [ObservableProperty]
    private string _footerModeText = string.Empty;

    [ObservableProperty]
    private string _footerPlaybackIcon = "play_white_icon.svg";

    [ObservableProperty]
    private bool _isFooterPlaybackEnabled;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Location> FeaturedLocations { get; } = new();
    public ObservableCollection<Location> MoreLocations { get; } = new();
    public ObservableCollection<Location> FavoriteLocations { get; } = new();
    public ObservableCollection<FeaturedTourItem> FeaturedTours { get; } = new();

    public MainViewModel(
        INavigationService navigationService,
        IApiService apiService,
        IAudioService audioService,
        IGeolocationService geolocationService,
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _audioService = audioService;
        _geolocationService = geolocationService;
        _localizationService = localizationService;

        _localizationService.CultureChanged += (_, _) =>
        {
            _ = MainThread.InvokeOnMainThreadAsync(async () => await LoadDataAsync());
        };

        _audioService.StateChanged += OnAudioStateChanged;
        WeakReferenceMessenger.Default.Register<AutoAudioSelectionChangedMessage>(this, OnAutoAudioSelectionChanged);

        // Raise preview property when FavoriteLocations changes
        FavoriteLocations.CollectionChanged += (_, _) => OnPropertyChanged(nameof(FavoriteLocationsPreview));

        _ = LoadDataAsync();
    }

    // Provide a limited view (max 4) for the grid preview
    public IEnumerable<Location> FavoriteLocationsPreview => FavoriteLocations.Take(4);

    [RelayCommand]
    private void ToggleFavorite(Location? location)
    {
        if (location is null) return;
        location.IsFavorite = !location.IsFavorite;

        // maintain FavoriteLocations collection (max 4)
        if (location.IsFavorite)
        {
            if (!FavoriteLocations.Contains(location))
            {
                // if more than 4, remove last
                if (FavoriteLocations.Count >= 4)
                    FavoriteLocations.RemoveAt(FavoriteLocations.Count - 1);
                FavoriteLocations.Insert(0, location);
            }
        }
        else
        {
            if (FavoriteLocations.Contains(location))
                FavoriteLocations.Remove(location);
        }
    }

    private async Task LoadDataAsync()
    {
        var categories = await _apiService.GetCategoriesAsync();
        var locations = await _apiService.GetLocationsAsync();
        var tours = await _apiService.GetToursAsync();

        // Update hero stats
        LocationCount = locations.Count;
        AudioGuideCount = locations.Sum(l => l.AudioGuides.Count);
        TourCount = tours.Count;
        CategoryCount = categories.Count;

        // Populate categories with location counts
        Categories.Clear();
        foreach (var cat in categories)
        {
            cat.LocationCount = locations.Count(l => l.CategoryId == cat.Id);
            Categories.Add(cat);
        }

        // Set category names on locations
        foreach (var loc in locations)
        {
            var cat = categories.FirstOrDefault(c => c.Id == loc.CategoryId);
            loc.CategoryName = cat?.Name ?? T("Main_OtherCategory");
        }

        // Featured locations (first 6, matching web)
        FeaturedLocations.Clear();
        foreach (var loc in locations.Take(6))
            FeaturedLocations.Add(loc);

        // More locations (rest, matching web "Khám phá thêm")
        MoreLocations.Clear();
        foreach (var loc in locations.Skip(6))
            MoreLocations.Add(loc);

        // Favorite locations: initially pick up to 4 locations with IsFavorite, otherwise first 4
        FavoriteLocations.Clear();
        var favs = locations.Where(l => l.IsFavorite).Take(4).ToList();
        if (!favs.Any())
            favs = locations.Take(4).ToList();
        foreach (var f in favs)
            FavoriteLocations.Add(f);

        // Featured tours (matching web)
        FeaturedTours.Clear();
        foreach (var tour in tours.Where(t => t.IsFeatured))
        {
            FeaturedTours.Add(new FeaturedTourItem
            {
                Id = tour.Id,
                Name = tour.Name,
                Description = tour.Description,
                ImageUrl = tour.ImageUrl,
                Duration = tour.Duration,
                DurationText = FormatDuration(tour.Duration),
                LocationCount = tour.LocationIds.Count,
                PriceText = tour.Price == 0 ? T("Main_Free") : F("Main_PriceFormat", tour.Price)
            });
        }
    }

    public async Task OnAppearingAsync()
    {
        if (!Preferences.Get("AutoNearestPoiPlayback", true))
        {
            FooterStatusText = T("Footer_StandbyAutoOff");
            FooterHintText = T("Footer_EnableInSettings");
            FooterActionText = T("Footer_EnableManual");
            IsFooterActionEnabled = true;
            FooterModeText = T("Footer_StandbyOffMode");
            UpdateFooterPlaybackUi();
            return;
        }

        if (!_hasInitializedAutoAudio)
        {
            _hasInitializedAutoAudio = true;
            await StartAutoNearestAudioAsync();
            return;
        }

        var userLocation = await GetUserLocationAsync();
        if (!userLocation.HasValue)
        {
            return;
        }

        var nearestPoi = await FindNearestPoiAsync(userLocation.Value.Latitude, userLocation.Value.Longitude);
        if (!nearestPoi.HasValue)
        {
            return;
        }

        var (location, distanceMeters) = nearestPoi.Value;
        if (!string.Equals(AutoLocationId, location.Id, StringComparison.OrdinalIgnoreCase))
        {
            await PlayAutoAudioForLocationAsync(location.Id, distanceMeters);
            return;
        }

        await SyncAutoAudioSelectionWithUserPreferenceAsync(location.Id);
        FooterHintText = F("Footer_NearestHintFormat", Math.Round(distanceMeters));
        _currentPoiDistanceMeters = distanceMeters;
        UpdateFooterPlaybackUi();
    }

    private string FormatDuration(int minutes)
    {
        if (minutes < 60)
            return F("Main_DurationMinutesFormat", minutes);
        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0
            ? F("Main_DurationHoursMinutesFormat", hours, mins)
            : F("Main_DurationHoursOnlyFormat", hours);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return;

        await _navigationService.NavigateToAsync(nameof(Views.SearchPage),
            new Dictionary<string, object>
            {
                { "InitialQuery", SearchText.Trim() }
            });
    }

    [RelayCommand]
    private async Task CategorySelectedAsync(Category? category)
    {
        if (category is null) return;

        // Toggle selected state and deselect others
        foreach (var cat in Categories)
        {
            cat.IsSelected = (cat.Id == category.Id);
        }

        await _navigationService.NavigateToAsync(nameof(Views.SearchPage),
            new Dictionary<string, object>
            {
                { "InitialCategoryId", category.Id }
            });
    }

    [RelayCommand]
    private async Task LocationSelectedAsync(Location? location)
    {
        location ??= SelectedLocation;
        if (location is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object>
            {
                { "LocationId", location.Id }
            });

        SelectedLocation = null;
    }

    [RelayCommand]
    private async Task TourSelectedAsync(FeaturedTourItem? tour)
    {
        if (tour is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.TourDetailPage),
            new Dictionary<string, object> { { "TourId", tour.Id } });
    }

    [RelayCommand]
    private async Task ViewAllLocationsAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.SearchPage));
    }

    [RelayCommand]
    private async Task ViewAllToursAsync()
    {
        await _navigationService.NavigateToAsync("//ToursPage");
    }

    [RelayCommand]
    private async Task OpenAutoAudioPlayerAsync()
    {
        if (!Preferences.Get("AutoNearestPoiPlayback", true))
        {
            Preferences.Set("AutoNearestPoiPlayback", true);
            await StartAutoNearestAudioAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(AutoLocationId) && IsFooterActionEnabled)
        {
            await SyncAutoAudioSelectionWithUserPreferenceAsync(AutoLocationId);
            await _navigationService.NavigateToAsync(nameof(Views.AudioPlayerPage),
                new Dictionary<string, object>
                {
                    { "LocationId", AutoLocationId },
                    { "AudioGuideId", AutoAudioGuideId },
                    { "AudioUrl", AutoAudioUrl },
                    { "PlaybackSource", "AutoNearest" }
                });
            return;
        }

        await StartAutoNearestAudioAsync();
    }

    [RelayCommand]
    private async Task ToggleFooterPlaybackAsync()
    {
        if (!Preferences.Get("AutoNearestPoiPlayback", true))
        {
            Preferences.Set("AutoNearestPoiPlayback", true);
            await StartAutoNearestAudioAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(AutoLocationId))
        {
            await SyncAutoAudioSelectionWithUserPreferenceAsync(AutoLocationId);
        }

        if (string.IsNullOrWhiteSpace(AutoAudioUrl))
        {
            await StartAutoNearestAudioAsync();
            return;
        }

        var isCurrentAutoAudio = !string.IsNullOrWhiteSpace(_audioService.CurrentAudioUrl)
                                 && string.Equals(_audioService.CurrentAudioUrl, AutoAudioUrl, StringComparison.OrdinalIgnoreCase);

        if (isCurrentAutoAudio && _audioService.IsPlaying)
        {
            await _audioService.PauseAsync();
            return;
        }

        if (isCurrentAutoAudio)
        {
            await _audioService.ResumeAsync();
            return;
        }

        await _audioService.PlayAsync(AutoAudioUrl);
    }

    private async Task StartAutoNearestAudioAsync()
    {
        if (!Preferences.Get("AutoNearestPoiPlayback", true))
        {
            FooterStatusText = T("Footer_StandbyAutoOff");
            FooterHintText = T("Footer_EnableInSettings");
            FooterActionText = T("Footer_EnableManual");
            IsFooterActionEnabled = true;
            FooterModeText = T("Footer_StandbyOffMode");
            UpdateFooterPlaybackUi();
            return;
        }

        FooterStatusText = T("Footer_StandbyLocating");
        FooterHintText = T("Footer_KeepGpsOn");
        FooterActionText = T("Footer_Retry");
        IsFooterActionEnabled = false;
        FooterModeText = T("Footer_StandbyOnMode");
        UpdateFooterPlaybackUi();

        // Step 1: Start geolocation tracking
        await _geolocationService.StartTrackingAsync();
        if (!_isGeoTrackingSubscribed)
        {
            _geolocationService.NearbyLocationDetected += OnNearbyLocationDetected;
            _isGeoTrackingSubscribed = true;
        }

        try
        {
            // Step 2: Get user location
            var userLocation = await GetUserLocationAsync();
            if (!userLocation.HasValue)
            {
                return;
            }

            // Step 3: Find nearest POI
            var nearestPoi = await FindNearestPoiAsync(userLocation.Value.Latitude, userLocation.Value.Longitude);
            if (nearestPoi == null)
            {
                return;
            }

            // Step 4: Trigger auto-play for the nearest POI
            var (location, distanceMeters) = nearestPoi.Value;
            await PlayAutoAudioForLocationAsync(
                location.Id,
                distanceMeters
            );
        }
        catch
        {
            FooterStatusText = T("Footer_AutoPlayError");
            FooterHintText = T("Footer_OpenDetailManual");
            FooterActionText = T("Footer_Retry");
            IsFooterActionEnabled = true;
            UpdateFooterPlaybackUi();
        }
    }

    /// <summary>
    /// Step 1: Get current user location from geolocation service.
    /// </summary>
    private async Task<(double Latitude, double Longitude)?> GetUserLocationAsync()
    {
        try
        {
            var userLocation = await _geolocationService.GetCurrentLocationAsync();
            if (!userLocation.HasValue)
            {
                FooterStatusText = T("Footer_NoLocation");
                FooterHintText = T("Footer_EnableLocationPermission");
                FooterActionText = T("Footer_Retry");
                IsFooterActionEnabled = true;
                return null;
            }
            return userLocation;
        }
        catch
        {
            FooterStatusText = T("Footer_LocationError");
            FooterHintText = T("Footer_CheckGps");
            FooterActionText = T("Footer_Retry");
            IsFooterActionEnabled = true;
            return null;
        }
    }

    /// <summary>
    /// Step 2: Find the nearest POI from the user's current location.
    /// </summary>
    private async Task<(Location Location, double DistanceMeters)?> FindNearestPoiAsync(double userLatitude, double userLongitude)
    {
        try
        {
            var locations = await _apiService.GetLocationsAsync();
            if (locations.Count == 0)
            {
                FooterStatusText = T("Footer_NoNearbyPoiAutoPlay");
                FooterHintText = T("Footer_NoPoiData");
                FooterActionText = T("Footer_Waiting");
                IsFooterActionEnabled = false;
                return null;
            }

            var nearest = locations
                .Select(loc => new
                {
                    Location = loc,
                    DistanceMeters = CalculateDistanceMeters(
                        userLatitude,
                        userLongitude,
                        loc.Latitude,
                        loc.Longitude),
                    PoiRadiusMeters = Math.Max(loc.DetectionRadiusMeters, 0)
                })
                .Where(x => x.DistanceMeters <= DefaultUserScanRadiusMeters + x.PoiRadiusMeters)
                .OrderBy(x => x.DistanceMeters)
                .ThenByDescending(x => x.Location.Priority)
                .ThenBy(x => x.Location.Id, StringComparer.OrdinalIgnoreCase)
                .First();

            if (nearest is null)
            {
                FooterStatusText = T("Footer_NoNearbyPoiAutoPlay");
                FooterHintText = T("Footer_NoPoiData");
                FooterActionText = T("Footer_Waiting");
                IsFooterActionEnabled = false;
                return null;
            }

            return (nearest.Location, nearest.DistanceMeters);
        }
        catch
        {
            FooterStatusText = T("Footer_FindNearestError");
            FooterHintText = T("Footer_PleaseRetry");
            FooterActionText = T("Footer_Retry");
            IsFooterActionEnabled = true;
            return null;
        }
    }

    /// <summary>
    /// Step 3: Trigger auto-play for a specific location with available audio.
    /// </summary>
    private async Task PlayAutoAudioForLocationAsync(string locationId, double distanceMeters)
    {
        await _autoSwitchLock.WaitAsync();
        try
        {
            var locations = await _apiService.GetLocationsAsync();
            var location = locations.FirstOrDefault(l => l.Id == locationId);
            if (location == null)
            {
                return;
            }

            var previousLocationId = AutoLocationId;
            var isSwitchingPoi = !string.IsNullOrWhiteSpace(previousLocationId)
                                 && !string.Equals(previousLocationId, locationId, StringComparison.OrdinalIgnoreCase);

            if (isSwitchingPoi)
            {
                // New POI detected: clear current auto queue context and stop current audio before loading new queue.
                ClearAutoQueueSelection();
                await _audioService.StopAsync();
            }

            var queue = await ResolveAutoAudioQueueAsync(locationId);

            AutoLocationId = location.Id;
            AutoLocationName = location.Name;
            _currentPoiDistanceMeters = distanceMeters;

            if (queue.Count == 0)
            {
                FooterStatusText = F("Footer_NoAudioAtPoiFormat", AutoLocationName);
                FooterHintText = T("Footer_WaitForNewPoiAudio");
                FooterActionText = T("Footer_Waiting");
                IsFooterActionEnabled = false;
                ClearAutoQueueSelection();
                UpdateFooterPlaybackUi();
                return;
            }

            _autoQueue.Clear();
            _autoQueue.AddRange(queue);

            _autoQueueIndex = 0;
            ApplyAutoQueueItem(_autoQueueIndex);

            // Update footer status
            FooterStatusText = F("Footer_PlayingAutoFormat", AutoLocationName);
            FooterHintText = BuildQueueProgressHint(distanceMeters);
            FooterActionText = T("Footer_OpenPlayer");
            IsFooterActionEnabled = true;
            UpdateFooterPlaybackUi();

            // Start playing nearest POI audio.
            if (!string.Equals(_audioService.CurrentAudioUrl, AutoAudioUrl, StringComparison.OrdinalIgnoreCase) || !_audioService.IsPlaying)
            {
                await _audioService.PlayAsync(AutoAudioUrl);
            }
        }
        catch
        {
            FooterStatusText = T("Footer_PlayAudioError");
            FooterHintText = T("Footer_PleaseRetry");
            FooterActionText = T("Footer_Retry");
            IsFooterActionEnabled = true;
            UpdateFooterPlaybackUi();
        }
        finally
        {
            _autoSwitchLock.Release();
        }
    }

    private async void OnNearbyLocationDetected(object? sender, NearbyLocationEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!Preferences.Get("AutoNearestPoiPlayback", true))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(e.LocationId))
            {
                FooterStatusText = T("Footer_NoNearbyPoiAutoPlay");
                FooterHintText = T("Footer_NoPoiData");
                FooterActionText = T("Footer_Waiting");
                IsFooterActionEnabled = false;
                UpdateFooterPlaybackUi();
                return;
            }

            if (string.Equals(AutoLocationId, e.LocationId, StringComparison.OrdinalIgnoreCase))
            {
                FooterHintText = BuildQueueProgressHint(e.DistanceMeters);
                _currentPoiDistanceMeters = e.DistanceMeters;
                return;
            }

            // Apply debounce: prevent rapid successive geofence event processing (spam prevention).
            var timeSinceLastEvent = DateTime.UtcNow - _lastGeoEventProcessedUtc;
            if (timeSinceLastEvent.TotalSeconds < GeoEventDebounceSeconds)
            {
                return; // Debounce active, ignore this geofence event.
            }

            _lastGeoEventProcessedUtc = DateTime.UtcNow;
            await PlayAutoAudioForLocationAsync(e.LocationId, e.DistanceMeters);
        });
    }

    private async Task<List<AutoQueueItem>> ResolveAutoAudioQueueAsync(string locationId)
    {
        var guides = await _apiService.GetAudioGuidesForLocationAsync(locationId);
        guides = guides
            .OrderBy(GetAutoPlaybackOrderPriority)
            .ThenBy(g => g.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var queue = guides
            .Select(g => new AutoQueueItem(g.Id, ResolveAudioSource(g)))
            .Where(item => !string.IsNullOrWhiteSpace(item.AudioUrl))
            .ToList();

        return queue;
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var queueIndex = FindQueueIndexByAudioUrl(e.AudioUrl);
            var isCurrentAutoAudio = !string.IsNullOrWhiteSpace(AutoAudioUrl)
                                     && !string.IsNullOrWhiteSpace(e.AudioUrl)
                                     && string.Equals(AutoAudioUrl, e.AudioUrl, StringComparison.OrdinalIgnoreCase);

            if (!isCurrentAutoAudio && queueIndex < 0)
            {
                return;
            }

            // Ignore stale stop events from old audio (for example while user manually switches track).
            if (!isCurrentAutoAudio && e.State == AudioPlaybackState.Stopped)
            {
                UpdateFooterPlaybackUi();
                return;
            }

            if (queueIndex >= 0
                && queueIndex != _autoQueueIndex
                && (isCurrentAutoAudio || e.State == AudioPlaybackState.Playing))
            {
                ApplyAutoQueueItem(queueIndex);
            }

            switch (e.State)
            {
                case AudioPlaybackState.Loading:
                    FooterStatusText = T("Footer_LoadingNearest");
                    break;
                case AudioPlaybackState.Playing:
                    if (!string.IsNullOrWhiteSpace(AutoLocationName))
                    {
                        FooterStatusText = F("Footer_PlayingAutoFormat", AutoLocationName);
                    }
                    FooterHintText = BuildQueueProgressHint(_currentPoiDistanceMeters);
                    FooterActionText = T("Footer_OpenPlayer");
                    IsFooterActionEnabled = true;
                    break;
                case AudioPlaybackState.Paused:
                    FooterStatusText = T("Footer_AudioPaused");
                    FooterHintText = BuildQueueProgressHint(_currentPoiDistanceMeters);
                    FooterActionText = T("Footer_Resume");
                    IsFooterActionEnabled = !string.IsNullOrWhiteSpace(AutoLocationId);
                    break;
                case AudioPlaybackState.Stopped:
                    FooterStatusText = T("Footer_SwitchingNext");
                    FooterHintText = BuildQueueProgressHint(_currentPoiDistanceMeters);
                    FooterActionText = T("Footer_OpenPlayer");
                    IsFooterActionEnabled = !string.IsNullOrWhiteSpace(AutoLocationId);
                    break;
                case AudioPlaybackState.Error:
                    FooterStatusText = T("Footer_PlayAudioError");
                    FooterActionText = T("Footer_Retry");
                    IsFooterActionEnabled = true;
                    break;
            }

            UpdateFooterPlaybackUi();

            // Only auto-advance when the currently selected auto-audio finished naturally.
            if (e.State == AudioPlaybackState.Stopped && queueIndex >= 0 && isCurrentAutoAudio)
            {
                _ = AdvanceAutoQueueAfterStopAsync(e.AudioUrl);
            }
        });
    }

    private void OnAutoAudioSelectionChanged(object recipient, AutoAudioSelectionChangedMessage message)
    {
        var payload = message.Value;
        Preferences.Set(GetPreferredAudioGuideKey(payload.LocationId), payload.AudioGuideId);
        Preferences.Set(GetPreferredAudioUrlKey(payload.LocationId), payload.AudioUrl);

        if (!string.Equals(AutoLocationId, payload.LocationId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var selectedIndex = _autoQueue.FindIndex(item =>
                string.Equals(item.AudioGuideId, payload.AudioGuideId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.AudioUrl, payload.AudioUrl, StringComparison.OrdinalIgnoreCase));

            if (selectedIndex >= 0)
            {
                ApplyAutoQueueItem(selectedIndex);
            }
            else
            {
                AutoAudioGuideId = payload.AudioGuideId;
                AutoAudioUrl = payload.AudioUrl;
            }

            if (!string.IsNullOrWhiteSpace(payload.LocationName))
            {
                AutoLocationName = payload.LocationName;
            }

            FooterActionText = T("Footer_OpenPlayer");
            IsFooterActionEnabled = true;
            FooterStatusText = _audioService.IsPlaying
                ? F("Footer_PlayingAutoFormat", AutoLocationName)
                : F("Footer_UpdatedAudioFormat", AutoLocationName);
            FooterHintText = BuildQueueProgressHint(_currentPoiDistanceMeters);
            UpdateFooterPlaybackUi();
        });
    }

    private async Task SyncAutoAudioSelectionWithUserPreferenceAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return;
        }

        var queue = await ResolveAutoAudioQueueAsync(locationId);
        if (queue.Count == 0)
        {
            return;
        }

        _autoQueue.Clear();
        _autoQueue.AddRange(queue);

        var serviceAudioIndex = _autoQueue.FindIndex(item =>
            string.Equals(item.AudioUrl, _audioService.CurrentAudioUrl, StringComparison.OrdinalIgnoreCase));

        if (serviceAudioIndex >= 0)
        {
            _autoQueueIndex = serviceAudioIndex;
        }
        else if (_autoQueueIndex >= 0 && _autoQueueIndex < _autoQueue.Count)
        {
            // Keep current auto queue position if we are still at the same POI.
        }
        else
        {
            var preferredIndex = ResolvePreferredQueueIndex(locationId, _autoQueue);
            _autoQueueIndex = preferredIndex >= 0 ? preferredIndex : 0;
        }

        ApplyAutoQueueItem(_autoQueueIndex);
    }

    private static int ResolvePreferredQueueIndex(string locationId, IReadOnlyList<AutoQueueItem> queue)
    {
        if (queue.Count == 0)
        {
            return -1;
        }

        var preferredGuideId = Preferences.Get(GetPreferredAudioGuideKey(locationId), string.Empty);
        if (!string.IsNullOrWhiteSpace(preferredGuideId))
        {
            var byGuideId = queue
                .Select((item, index) => new { item, index })
                .FirstOrDefault(x => string.Equals(x.item.AudioGuideId, preferredGuideId, StringComparison.OrdinalIgnoreCase));
            if (byGuideId != null)
            {
                return byGuideId.index;
            }
        }

        var preferredAudioUrl = Preferences.Get(GetPreferredAudioUrlKey(locationId), string.Empty);
        if (!string.IsNullOrWhiteSpace(preferredAudioUrl))
        {
            var byAudioUrl = queue
                .Select((item, index) => new { item, index })
                .FirstOrDefault(x => string.Equals(x.item.AudioUrl, preferredAudioUrl, StringComparison.OrdinalIgnoreCase));
            if (byAudioUrl != null)
            {
                return byAudioUrl.index;
            }
        }

        return -1;
    }

    private int FindQueueIndexByAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return -1;
        }

        return _autoQueue.FindIndex(item =>
            string.Equals(item.AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyAutoQueueItem(int queueIndex)
    {
        if (queueIndex < 0 || queueIndex >= _autoQueue.Count)
        {
            return;
        }

        _autoQueueIndex = queueIndex;
        var currentItem = _autoQueue[queueIndex];
        AutoAudioGuideId = currentItem.AudioGuideId;
        AutoAudioUrl = currentItem.AudioUrl;
    }

    private void ClearAutoQueueSelection()
    {
        _autoQueue.Clear();
        _autoQueueIndex = -1;
        AutoAudioGuideId = string.Empty;
        AutoAudioUrl = string.Empty;
    }

    private async Task AdvanceAutoQueueAfterStopAsync(string? stoppedAudioUrl)
    {
        if (!Preferences.Get("AutoNearestPoiPlayback", true))
        {
            return;
        }

        await _autoSwitchLock.WaitAsync();
        try
        {
            var stoppedIndex = FindQueueIndexByAudioUrl(stoppedAudioUrl);
            if (stoppedIndex < 0)
            {
                return;
            }

            var nextIndex = stoppedIndex + 1;
            if (nextIndex >= _autoQueue.Count)
            {
                _autoQueueIndex = _autoQueue.Count - 1;
                FooterStatusText = F("Footer_FinishedAtPoiFormat", AutoLocationName);
                FooterHintText = T("Footer_WaitForNewNearest");
                FooterActionText = T("Footer_OpenPlayer");
                IsFooterActionEnabled = !string.IsNullOrWhiteSpace(AutoLocationId);
                UpdateFooterPlaybackUi();
                return;
            }

            ApplyAutoQueueItem(nextIndex);
            FooterStatusText = F("Footer_PlayingAutoFormat", AutoLocationName);
            FooterHintText = BuildQueueProgressHint(_currentPoiDistanceMeters);
            FooterActionText = T("Footer_OpenPlayer");
            IsFooterActionEnabled = true;
            UpdateFooterPlaybackUi();
            await _audioService.PlayAsync(AutoAudioUrl);
        }
        finally
        {
            _autoSwitchLock.Release();
        }
    }

    private string BuildQueueProgressHint(double distanceMeters)
    {
        var distanceHint = distanceMeters < double.MaxValue
            ? F("Footer_NearestHintFormat", Math.Round(distanceMeters))
            : T("Footer_NearestShort");

        if (_autoQueue.Count <= 0 || _autoQueueIndex < 0)
        {
            return distanceHint;
        }

        return F("Footer_QueueHintFormat", distanceHint, _autoQueueIndex + 1, _autoQueue.Count);
    }

    private static string ResolveAudioSource(AudioGuide guide)
    {
        return !string.IsNullOrWhiteSpace(guide.CloudinaryAudioUrl)
            ? guide.CloudinaryAudioUrl
            : guide.AudioUrl;
    }

    private static int GetAutoPlaybackOrderPriority(AudioGuide guide)
    {
        var id = guide.Id ?? string.Empty;
        var title = guide.Title ?? string.Empty;

        if (id.EndsWith("_1", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Giới thiệu", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Introduction", StringComparison.OrdinalIgnoreCase)
            || title.Contains("介绍", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (id.EndsWith("_2", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Khám phá", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Discovery", StringComparison.OrdinalIgnoreCase)
            || title.Contains("探索", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }

    private static string GetPreferredAudioGuideKey(string locationId) => $"{PreferredAudioGuideKeyPrefix}{locationId}";

    private static string GetPreferredAudioUrlKey(string locationId) => $"{PreferredAudioUrlKeyPrefix}{locationId}";

    private string T(string key) => _localizationService.GetString(key);

    private string F(string key, params object[] args)
    {
        var template = T(key);
        return string.Format(template, args);
    }

    private void UpdateFooterPlaybackUi()
    {
        var hasAutoAudio = !string.IsNullOrWhiteSpace(AutoAudioUrl);
        var isCurrentAutoAudio = hasAutoAudio
                                 && !string.IsNullOrWhiteSpace(_audioService.CurrentAudioUrl)
                                 && string.Equals(_audioService.CurrentAudioUrl, AutoAudioUrl, StringComparison.OrdinalIgnoreCase);

        IsFooterPlaybackEnabled = true;
        FooterPlaybackIcon = isCurrentAutoAudio && _audioService.IsPlaying
            ? "pause.svg"
            : "play_white_icon.svg";
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c * 1000;
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;

    private sealed record AutoQueueItem(string AudioGuideId, string AudioUrl);
}

public class FeaturedTourItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationText { get; set; } = string.Empty;
    public int LocationCount { get; set; }
    public string PriceText { get; set; } = string.Empty;
}
