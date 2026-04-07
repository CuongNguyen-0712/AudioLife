using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;
using LocationModel = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private const double NearbyRadiusKm = 0.1;
    private const int MaxSuggestions = 10;

    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;
    private readonly IGeolocationService _geolocationService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private double _userLatitude = 21.0285;

    [ObservableProperty]
    private double _userLongitude = 105.8542;

    [ObservableProperty]
    private HtmlWebViewSource? _mapHtmlSource;

    [ObservableProperty]
    private string _locationStatusText = "Đang xác định vị trí...";

    [ObservableProperty]
    private bool _isLocationConfirmed = false;

    public ObservableCollection<MapMarker> MapMarkers { get; } = new();
    public ObservableCollection<NearbyLocation> NearbyLocations { get; } = new();

    public MapViewModel(INavigationService navigationService, IApiService apiService, IGeolocationService geolocationService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _geolocationService = geolocationService;
    }

    [RelayCommand]
    public async Task LoadMapDataAsync()
    {
        await _geolocationService.StartTrackingAsync();

        // Try getting real current location
        var location = await _geolocationService.GetCurrentLocationAsync();
        if (location.HasValue)
        {
            UserLatitude = location.Value.Latitude;
            UserLongitude = location.Value.Longitude;
            IsLocationConfirmed = true;
            LocationStatusText = $"Vị trí hiện tại: {location.Value.Latitude:F6}, {location.Value.Longitude:F6}";
        }
        else if (_geolocationService.CurrentLatitude.HasValue && _geolocationService.CurrentLongitude.HasValue)
        {
            UserLatitude = _geolocationService.CurrentLatitude.Value;
            UserLongitude = _geolocationService.CurrentLongitude.Value;
            IsLocationConfirmed = true;
            LocationStatusText = $"Vị trí hiện tại: {UserLatitude:F6}, {UserLongitude:F6}";
        }
        else
        {
            IsLocationConfirmed = false;
            LocationStatusText = "Không thể xác định vị trí. Sử dụng vị trí mặc định.";
        }

        var locations = await _apiService.GetLocationsAsync();
        var categories = await _apiService.GetCategoriesAsync();

        MapMarkers.Clear();
        NearbyLocations.Clear();

        foreach (var loc in locations)
        {
            MapMarkers.Add(new MapMarker
            {
                Id = loc.Id,
                Name = loc.Name,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude
            });
        }

        var sortedByDistance = locations
            .Select(loc => new
            {
                Location = loc,
                DistanceKm = CalculateDistance(UserLatitude, UserLongitude, loc.Latitude, loc.Longitude)
            })
            .OrderBy(x => x.DistanceKm)
            .ToList();

        var nearby = sortedByDistance
            .Where(x => x.DistanceKm <= NearbyRadiusKm)
            .Take(MaxSuggestions)
            .ToList();

        if (nearby.Count > 0)
        {
            foreach (var item in nearby)
            {
                NearbyLocations.Add(new NearbyLocation
                {
                    Id = item.Location.Id,
                    Name = item.Location.Name,
                    ImageUrl = item.Location.ImageUrl,
                    Distance = Math.Round(item.DistanceKm, 3),
                    DistanceString = FormatDistance(item.DistanceKm)
                });
            }

            LocationStatusText = $"{LocationStatusText} • {nearby.Count} POI trong bán kính 100m";
        }
        else
        {
            var hotLocations = GetHotLocations(locations).Take(MaxSuggestions).ToList();
            foreach (var hot in hotLocations)
            {
                NearbyLocations.Add(new NearbyLocation
                {
                    Id = hot.Id,
                    Name = hot.Name,
                    ImageUrl = hot.ImageUrl,
                    Distance = 0,
                    DistanceString = "🔥 POI hot"
                });
            }

            LocationStatusText = $"{LocationStatusText} • Không có POI trong 100m, đang gợi ý POI hot";
        }

        // Generate Leaflet map HTML
        GenerateMapHtml(locations, categories);
    }

    private void GenerateMapHtml(List<Models.Location> locations, List<Category> categories)
    {
        var markersJs = new StringBuilder();
        foreach (var loc in locations)
        {
            var cat = categories.FirstOrDefault(c => c.Id == loc.CategoryId);
            var escapedName = loc.Name.Replace("'", "\\'");
            var escapedAddr = loc.Address.Replace("'", "\\'");
            var escapedCat = (cat?.Name ?? "Khác").Replace("'", "\\'");
            markersJs.AppendLine(
                $"L.marker([{loc.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{loc.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{icon: customIcon}})" +
                $".addTo(map).bindPopup('<b>{escapedName}</b><br/>{escapedCat}<br/>📍 {escapedAddr}<br/>⏱ {loc.Duration} phút');");
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
    </style>
</head>
<body>
<div id='map'></div>
<script>
    var map = L.map('map').setView([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 14);
    L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }}).addTo(map);

    var customIcon = L.divIcon({{
        className: 'custom-marker',
        html: '<div style=""width:28px;height:28px;background:#512BD4;border-radius:50%;border:3px solid white;box-shadow:0 2px 6px rgba(0,0,0,0.3);""></div>',
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        popupAnchor: [0, -16]
    }});

    // User location marker
    var userIcon = L.divIcon({{
        className: 'user-marker',
        html: '<div style=""width:16px;height:16px;background:#4285F4;border-radius:50%;border:3px solid white;box-shadow:0 0 0 8px rgba(66,133,244,0.2);""></div>',
        iconSize: [16, 16],
        iconAnchor: [8, 8]
    }});
    
    var userMarker = L.marker([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{icon: userIcon}}).addTo(map).bindPopup('Vị trí của bạn');
    
    // Draw 100m radius circle
    var radiusCircle = L.circle([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{
        radius: 100,
        color: '#4285F4',
        weight: 1,
        fillColor: '#4285F4',
        fillOpacity: 0.2
    }}).addTo(map);

    // Location markers
    {markersJs}
</script>
</body>
</html>";

        MapHtmlSource = new HtmlWebViewSource { Html = html };
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

    private static string FormatDistance(double distanceKm)
    {
        if (distanceKm < 1)
        {
            return $"{Math.Round(distanceKm * 1000)} m";
        }

        return $"{distanceKm:F2} km";
    }

    private static List<LocationModel> GetHotLocations(IEnumerable<LocationModel> locations)
    {
        var locationList = locations.ToList();
        var featuredTours = Data.SampleData.GetFeaturedTours();
        var scoreByLocationId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tour in featuredTours)
        {
            foreach (var locationId in tour.LocationIds)
            {
                scoreByLocationId[locationId] = scoreByLocationId.TryGetValue(locationId, out var score)
                    ? score + 3
                    : 3;
            }
        }

        foreach (var location in locationList)
        {
            if (location.IsFavorite)
            {
                scoreByLocationId[location.Id] = scoreByLocationId.TryGetValue(location.Id, out var score)
                    ? score + 2
                    : 2;
            }

            // Boost locations with more audio guides.
            scoreByLocationId[location.Id] = scoreByLocationId.TryGetValue(location.Id, out var current)
                ? current + Math.Max(1, location.AudioGuideCount)
                : Math.Max(1, location.AudioGuideCount);
        }

        return locationList
            .OrderByDescending(loc => scoreByLocationId.TryGetValue(loc.Id, out var score) ? score : 0)
            .ThenBy(loc => loc.Name)
            .ToList();
    }

    [RelayCommand]
    private async Task LocationTappedAsync(NearbyLocation? location)
    {
        if (location is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", location.Id } });
    }
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
    public double Distance { get; set; }
    public string DistanceString { get; set; } = string.Empty;
}
