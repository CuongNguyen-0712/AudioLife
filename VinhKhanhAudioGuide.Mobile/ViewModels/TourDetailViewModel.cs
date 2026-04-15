using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

[QueryProperty(nameof(TourId), "TourId")]
public partial class TourDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _tourId = string.Empty;

    [ObservableProperty]
    private string _tourName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _durationText = string.Empty;

    [ObservableProperty]
    private int _locationCount;

    [ObservableProperty]
    private string _priceText = string.Empty;

    [ObservableProperty]
    private HtmlWebViewSource? _mapHtmlSource;

    public ObservableCollection<TourLocationItem> TourLocations { get; } = new();

    public TourDetailViewModel(INavigationService navigationService, IApiService apiService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
    }

    partial void OnTourIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadTourDetailsAsync(value);
        }
    }

    private async Task LoadTourDetailsAsync(string tourId)
    {
        var tour = await _apiService.GetTourByIdAsync(tourId);

        if (tour == null)
        {
            var tours = await _apiService.GetToursAsync();
            tour = tours.ElementAtOrDefault(int.TryParse(tourId, out var idx) ? idx - 1 : -1);
        }

        if (tour == null) return;

        TourName = tour.Name;
        Description = tour.Description;
        ImageUrl = tour.ImageUrl;
        DurationText = FormatDuration(tour.Duration);
        LocationCount = tour.LocationIds.Count;

        var allLocationsTask = _apiService.GetLocationsAsync();
        var categoriesTask = _apiService.GetCategoriesAsync();
        await Task.WhenAll(allLocationsTask, categoriesTask);

        var allLocations = allLocationsTask.Result;
        var categories = categoriesTask.Result;
        TourLocations.Clear();

        int order = 1;
        var totalCount = tour.LocationIds.Count;
        var resolvedLocations = new List<(Models.Location loc, int order)>();

        foreach (var locationId in tour.LocationIds)
        {
            var location = allLocations.FirstOrDefault(l => l.Id == locationId);
            if (location != null)
            {
                var cat = categories.FirstOrDefault(c => c.Id == location.CategoryId);
                TourLocations.Add(new TourLocationItem
                {
                    Id = location.Id,
                    Name = location.Name,
                    Duration = location.Duration,
                    Order = order,
                    AudioCount = location.AudioGuides.Count,
                    CategoryName = cat?.Name ?? "Di tích",
                    IsNotLast = order < totalCount,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude
                });
                resolvedLocations.Add((location, order));
                order++;
            }
        }

        // Build map HTML with route
        BuildMapHtml(resolvedLocations);
    }

    private void BuildMapHtml(List<(Models.Location loc, int order)> locations)
    {
        if (locations.Count == 0) return;

        var centerLat = locations.Average(l => l.loc.Latitude);
        var centerLng = locations.Average(l => l.loc.Longitude);

        var markersJs = string.Join("\n", locations.Select(l =>
        {
            var lat = l.loc.Latitude.ToString(CultureInfo.InvariantCulture);
            var lng = l.loc.Longitude.ToString(CultureInfo.InvariantCulture);
            var name = l.loc.Name.Replace("'", "\\'");
            return "L.marker([" + lat + "," + lng + "],{icon:L.divIcon({className:'tour-poi-icon',html:'<div class=\"tour-poi-wrapper\"><img src=\"location_icon.svg\" class=\"tour-poi-pin\"/><div class=\"tour-poi-order\">" + l.order + "</div></div>',iconSize:[48,56],iconAnchor:[24,52],popupAnchor:[0,-48]})}).addTo(map).bindPopup('<div style=\"font-family:RobotoCondensed-Regular,-apple-system,Segoe UI,sans-serif;font-size:13px;line-height:1.35;color:#191C1B;\"><b>" + l.order + ". " + name + "</b></div>');";
        }));

        var polylineCoords = string.Join(",", locations.Select(l =>
            $"[{l.loc.Latitude.ToString(CultureInfo.InvariantCulture)},{l.loc.Longitude.ToString(CultureInfo.InvariantCulture)}]"));

        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'/>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>
html, body, #map {{ height:100%; margin:0; padding:0; }}

.custom-zoom {{
    position:absolute;
    left:12px;
    top:12px;
    z-index:1000;
    display:flex;
    flex-direction:column;
    gap:8px;
}}

.custom-zoom button {{
    width:38px;
    height:38px;
    border:0;
    border-radius:12px;
    background:#C5E6E8;
    color:#49686A;
    font-size:22px;
    line-height:1;
    font-weight:700;
    box-shadow:0 4px 12px rgba(0,0,0,0.16);
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
</style>
</head>
<body>
<div id='map'></div>
<div class='custom-zoom'>
    <button id='zoomInBtn' type='button'>+</button>
    <button id='zoomOutBtn' type='button'>-</button>
</div>
<script>
var map = L.map('map', {{
    zoomControl:false,
    dragging:true,
    touchZoom:true,
    scrollWheelZoom:true,
    doubleClickZoom:true,
    boxZoom:true
}}).setView([{centerLat.ToString(CultureInfo.InvariantCulture)},{centerLng.ToString(CultureInfo.InvariantCulture)}],14);

L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',{{
    maxZoom:19,
    attribution:'© OpenStreetMap'
}}).addTo(map);

document.getElementById('zoomInBtn').addEventListener('click', function() {{ map.zoomIn(); }});
document.getElementById('zoomOutBtn').addEventListener('click', function() {{ map.zoomOut(); }});

{markersJs}

var routeLine = L.polyline([{polylineCoords}],{{
    color:'#13696D',
    weight:4,
    opacity:0.82,
    lineJoin:'round'
}}).addTo(map);

var bounds = routeLine.getBounds();
if (bounds && bounds.isValid()) {{
    map.fitBounds(bounds.pad(0.18));
}}
</script>
</body>
</html>";

        MapHtmlSource = new HtmlWebViewSource { Html = html };
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60)
            return $"{minutes} phút";

        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"{hours}h {mins}p" : $"{hours} giờ";
    }

    [RelayCommand]
    private async Task LocationTappedAsync(TourLocationItem? item)
    {
        if (item is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", item.Id } });
    }

    [RelayCommand]
    private async Task OpenMapAsync()
    {
        await _navigationService.NavigateToAsync("//MapPage");
    }

    [RelayCommand]
    private async Task StartTourAsync()
    {
        var firstLocation = TourLocations.FirstOrDefault();
        if (firstLocation != null)
        {
            await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
                new Dictionary<string, object> { { "LocationId", firstLocation.Id } });
        }
    }
}

public class TourLocationItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int Order { get; set; }
    public int AudioCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsNotLast { get; set; } = true;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
