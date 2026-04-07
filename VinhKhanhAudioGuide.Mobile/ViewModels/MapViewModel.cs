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
    private readonly IGeolocationService _geolocationService;
    private readonly IApiService _apiService;

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

    public MapViewModel(INavigationService navigationService, IGeolocationService geolocationService, IApiService apiService)
    {
        _navigationService = navigationService;
        _geolocationService = geolocationService;
        _apiService = apiService;
        LoadMapData();
    }

    public void LoadMapData()
    {
        _ = LoadMapDataAsync();
    }

    public async Task LoadMapDataAsync()
    {
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

            var distance = CalculateDistance(UserLatitude, UserLongitude, point.Latitude, point.Longitude);
            var category = categories.FirstOrDefault(c => c.Id == location.CategoryId);
            NearbyLocations.Add(new NearbyLocation
            {
                Id = location.Id,
                Name = location.Name,
                ImageUrl = location.ImageUrl,
                CategoryName = category?.Name ?? "Khác",
                Address = location.Address,
                Distance = Math.Round(distance, 1),
                AudioCount = location.AudioGuides?.Count ?? 0,
                IsHot = featuredLocationIds.Contains(location.Id)
            });
        }

        // Sort by distance and keep all POIs in the list.
        var sorted = NearbyLocations.OrderBy(x => x.Distance).ToList();
        NearbyLocations.Clear();
        for (var i = 0; i < sorted.Count; i++)
        {
            var loc = sorted[i];
            loc.IsNearest = i == 0;
            NearbyLocations.Add(loc);
        }

        CurrentPoiLocation = NearbyLocations.FirstOrDefault(x => x.IsNearest) ?? NearbyLocations.FirstOrDefault();

        // Generate Leaflet map HTML
        GenerateMapHtml(locationPoints, categories);
    }

    private void GenerateMapHtml(List<LocationPoint> locations, List<Category> categories)
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
            var markerIcon = isNearest ? "nearestIcon" : "customIcon";
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
        html: '<div style=""width:28px;height:28px;background:{primary};border-radius:50%;border:3px solid {surfaceContainerLowest};box-shadow:0 2px 6px rgba(0,0,0,0.3);""></div>',
        iconSize: [28, 28],
        iconAnchor: [14, 14],
        popupAnchor: [0, -16]
    }});

    var nearestIcon = L.divIcon({{
        className: 'nearest-marker',
        html: '<div style=""width:32px;height:32px;background:{tertiary};border-radius:50%;border:3px solid {surfaceContainerLowest};box-shadow:0 4px 10px rgba(0,0,0,0.32);position:relative;""><div style=""position:absolute;inset:8px;background:{tertiaryFixed};border-radius:50%;opacity:0.5;""></div></div>',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
        popupAnchor: [0, -18]
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
}
