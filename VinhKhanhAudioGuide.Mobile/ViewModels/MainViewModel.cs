using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int AutoSwitchCooldownSeconds = 45;
    private const double MinDistanceImprovementMeters = 20;

    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;
    private readonly IAudioService _audioService;
    private readonly IGeolocationService _geolocationService;
    private bool _hasInitializedAutoAudio;
    private bool _isGeoTrackingSubscribed;
    private DateTimeOffset _lastAutoSwitchAt = DateTimeOffset.MinValue;
    private double _currentPoiDistanceMeters = double.MaxValue;

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

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Location> FeaturedLocations { get; } = new();
    public ObservableCollection<Location> MoreLocations { get; } = new();
    public ObservableCollection<Location> FavoriteLocations { get; } = new();
    public ObservableCollection<FeaturedTourItem> FeaturedTours { get; } = new();

    public MainViewModel(
        INavigationService navigationService,
        IApiService apiService,
        IAudioService audioService,
        IGeolocationService geolocationService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _audioService = audioService;
        _geolocationService = geolocationService;

        _audioService.StateChanged += OnAudioStateChanged;

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
            loc.CategoryName = cat?.Name ?? "Khác";
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
                PriceText = tour.Price == 0 ? "Miễn phí" : $"{tour.Price:N0} VNĐ"
            });
        }

        if (!_hasInitializedAutoAudio)
        {
            _hasInitializedAutoAudio = true;
            await StartAutoNearestAudioAsync();
        }
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
            return $"⏱ {minutes} phút";
        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"⏱ {hours}h {mins}p" : $"⏱ {hours} giờ";
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

        await _navigationService.NavigateToAsync("//SearchPage");
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

        await _navigationService.NavigateToAsync("//SearchPage");
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
        await _navigationService.NavigateToAsync("//SearchPage");
    }

    [RelayCommand]
    private async Task ViewAllToursAsync()
    {
        await _navigationService.NavigateToAsync("//ToursPage");
    }

    [RelayCommand]
    private async Task OpenAutoAudioPlayerAsync()
    {
        if (!string.IsNullOrWhiteSpace(AutoLocationId) && IsFooterActionEnabled)
        {
            await _navigationService.NavigateToAsync(nameof(Views.AudioPlayerPage),
                new Dictionary<string, object>
                {
                    { "LocationId", AutoLocationId },
                    { "AudioGuideId", AutoAudioGuideId },
                    { "AudioUrl", AutoAudioUrl }
                });
            return;
        }

        await StartAutoNearestAudioAsync();
    }

    private async Task StartAutoNearestAudioAsync()
    {
        FooterStatusText = "Chế độ chờ: Đang xác định vị trí...";
        FooterHintText = "Vui lòng giữ GPS bật để phát audio tự động theo POI gần nhất";
        FooterActionText = "Thử lại";
        IsFooterActionEnabled = false;
        FooterModeText = "Standby: ON";

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
                distanceMeters,
                $"POI gần nhất • {Math.Round(distanceMeters)} m"
            );
        }
        catch
        {
            FooterStatusText = "Chế độ chờ: Không thể phát audio tự động";
            FooterHintText = "Bạn có thể mở chi tiết POI và phát audio thủ công";
            FooterActionText = "Thử lại";
            IsFooterActionEnabled = true;
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
                FooterStatusText = "Chế độ chờ: Chưa lấy được vị trí";
                FooterHintText = "Bật quyền vị trí để hệ thống tự chọn POI gần nhất";
                FooterActionText = "Thử lại";
                IsFooterActionEnabled = true;
                return null;
            }
            return userLocation;
        }
        catch
        {
            FooterStatusText = "Chế độ chờ: Lỗi lấy vị trí";
            FooterHintText = "Vui lòng kiểm tra kết nối GPS";
            FooterActionText = "Thử lại";
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
                FooterStatusText = "Chế độ chờ: Không có POI";
                FooterHintText = "Hiện chưa có dữ liệu địa điểm để phát audio";
                FooterActionText = "Đang chờ";
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
                        loc.Longitude)
                })
                .OrderBy(x => x.DistanceMeters)
                .First();

            return (nearest.Location, nearest.DistanceMeters);
        }
        catch
        {
            FooterStatusText = "Chế độ chờ: Lỗi tìm POI gần nhất";
            FooterHintText = "Vui lòng thử lại";
            FooterActionText = "Thử lại";
            IsFooterActionEnabled = true;
            return null;
        }
    }

    /// <summary>
    /// Step 3: Trigger auto-play for a specific location with available audio.
    /// </summary>
    private async Task PlayAutoAudioForLocationAsync(string locationId, double distanceMeters, string hint)
    {
        try
        {
            // Resolve audio for the location
            var payload = await ResolveAutoAudioAsync(locationId);
            if (payload is null)
            {
                FooterStatusText = $"Chế độ chờ: POI gần nhất";
                FooterHintText = "POI gần nhất chưa có audio để phát";
                FooterActionText = "Đang chờ";
                IsFooterActionEnabled = false;
                return;
            }

            // Update location and audio information
            var locations = await _apiService.GetLocationsAsync();
            var location = locations.FirstOrDefault(l => l.Id == locationId);
            if (location == null)
            {
                return;
            }

            var previousLocationId = AutoLocationId;

            AutoLocationId = location.Id;
            AutoLocationName = location.Name;
            AutoAudioGuideId = payload.Value.AudioGuideId;
            AutoAudioUrl = payload.Value.AudioUrl;

            // Update footer status
            FooterStatusText = $"Đang phát tự động: {AutoLocationName}";
            FooterHintText = hint;
            FooterActionText = "Mở trình phát";
            IsFooterActionEnabled = true;
            _currentPoiDistanceMeters = distanceMeters;
            _lastAutoSwitchAt = DateTimeOffset.UtcNow;

            // Ensure only one auto audio is active: stop old POI audio before playing new POI audio.
            var isSwitchingPoi = !string.IsNullOrWhiteSpace(previousLocationId) &&
                                 !string.Equals(previousLocationId, locationId, StringComparison.OrdinalIgnoreCase);
            if (isSwitchingPoi)
            {
                await _audioService.StopAsync();
            }

            // Start playing nearest POI audio.
            if (!string.Equals(_audioService.CurrentAudioUrl, AutoAudioUrl, StringComparison.OrdinalIgnoreCase) || !_audioService.IsPlaying)
            {
                await _audioService.PlayAsync(AutoAudioUrl);
            }
        }
        catch
        {
            FooterStatusText = "Chế độ chờ: Lỗi phát audio";
            FooterHintText = "Vui lòng thử lại";
            FooterActionText = "Thử lại";
            IsFooterActionEnabled = true;
        }
    }

    private async void OnNearbyLocationDetected(object? sender, NearbyLocationEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var latitude = _geolocationService.CurrentLatitude;
            var longitude = _geolocationService.CurrentLongitude;
            if (!latitude.HasValue || !longitude.HasValue)
            {
                return;
            }

            var nearestPoi = await FindNearestPoiAsync(latitude.Value, longitude.Value);
            if (!nearestPoi.HasValue)
            {
                return;
            }

            var (nearestLocation, nearestDistanceMeters) = nearestPoi.Value;
            if (string.Equals(AutoLocationId, nearestLocation.Id, StringComparison.OrdinalIgnoreCase))
            {
                FooterHintText = $"POI gần nhất • {Math.Round(nearestDistanceMeters)} m";
                _currentPoiDistanceMeters = nearestDistanceMeters;
                return;
            }

            // Trigger nearest POI auto-audio and stop old POI audio before switching.
            var hint = $"POI mới trong vùng gần • {Math.Round(nearestDistanceMeters)} m";
            await PlayAutoAudioForLocationAsync(nearestLocation.Id, nearestDistanceMeters, hint);
        });
    }

    private async Task<(string AudioGuideId, string AudioUrl)?> ResolveAutoAudioAsync(string locationId)
    {
        var guides = await _apiService.GetAudioGuidesForLocationAsync(locationId);
        var selectedGuide = guides.FirstOrDefault(g =>
            !string.IsNullOrWhiteSpace(g.CloudinaryAudioUrl) ||
            !string.IsNullOrWhiteSpace(g.AudioUrl));

        if (selectedGuide is null)
        {
            return null;
        }

        var audioSource = !string.IsNullOrWhiteSpace(selectedGuide.CloudinaryAudioUrl)
            ? selectedGuide.CloudinaryAudioUrl
            : selectedGuide.AudioUrl;

        if (string.IsNullOrWhiteSpace(audioSource))
        {
            return null;
        }

        return (selectedGuide.Id, audioSource);
    }

    private void OnAudioStateChanged(object? sender, AudioStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (e.State)
            {
                case AudioPlaybackState.Loading:
                    FooterStatusText = "Chế độ chờ: Đang nạp audio gần nhất...";
                    break;
                case AudioPlaybackState.Playing:
                    if (!string.IsNullOrWhiteSpace(AutoLocationName))
                    {
                        FooterStatusText = $"Đang phát tự động: {AutoLocationName}";
                    }
                    FooterActionText = "Mở trình phát";
                    IsFooterActionEnabled = true;
                    break;
                case AudioPlaybackState.Paused:
                    FooterStatusText = "Chế độ chờ: Audio đang tạm dừng";
                    FooterActionText = "Tiếp tục nghe";
                    IsFooterActionEnabled = !string.IsNullOrWhiteSpace(AutoLocationId);
                    break;
                case AudioPlaybackState.Stopped:
                    FooterStatusText = "Chế độ chờ: Audio đã dừng";
                    FooterActionText = "Mở trình phát";
                    IsFooterActionEnabled = !string.IsNullOrWhiteSpace(AutoLocationId);
                    break;
                case AudioPlaybackState.Error:
                    FooterStatusText = "Chế độ chờ: Lỗi phát audio";
                    FooterActionText = "Thử lại";
                    IsFooterActionEnabled = true;
                    break;
            }
        });
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
