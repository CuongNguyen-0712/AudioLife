using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

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

    public MapViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        LoadMapData();
    }

    private void LoadMapData()
    {
        var locations = Data.SampleData.GetLocations();
        var categories = Data.SampleData.GetCategories();

        foreach (var location in locations)
        {
            MapMarkers.Add(new MapMarker
            {
                Id = location.Id,
                Name = location.Name,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            });

            var distance = CalculateDistance(UserLatitude, UserLongitude, location.Latitude, location.Longitude);
            var category = categories.FirstOrDefault(c => c.Id == location.CategoryId);
            NearbyLocations.Add(new NearbyLocation
            {
                Id = location.Id,
                Name = location.Name,
                ImageUrl = location.ImageUrl,
                CategoryName = category?.Name ?? "Khác",
                Address = location.Address,
                Distance = Math.Round(distance, 1)
            });
        }

        // Sort by distance
        var sorted = NearbyLocations.OrderBy(x => x.Distance).ToList();
        NearbyLocations.Clear();
        foreach (var loc in sorted.Take(5))
        {
            NearbyLocations.Add(loc);
        }

        CurrentPoiLocation = NearbyLocations.FirstOrDefault();

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
            background: #C5E6E8;
            opacity: 0.8;
            color: #49686A;
            font-size: 24px;
            line-height: 1;
            font-weight: 700;
            box-shadow: 0 4px 12px rgba(0,0,0,0.16);
            cursor: pointer;
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
    L.marker([{UserLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {UserLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], {{icon: userIcon}}).addTo(map).bindPopup('📍 Vị trí của bạn');

    document.getElementById('zoomInBtn').addEventListener('click', function() {{ map.zoomIn(); }});
    document.getElementById('zoomOutBtn').addEventListener('click', function() {{ map.zoomOut(); }});

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
    public string CategoryName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Distance { get; set; }
}
