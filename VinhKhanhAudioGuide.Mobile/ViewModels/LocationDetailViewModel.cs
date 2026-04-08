using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public class RelatedTourItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
}

[QueryProperty(nameof(LocationId), "LocationId")]
public partial class LocationDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _locationId = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _locationName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _imageUrl = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private int _duration;

    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _audioGuideCountText = string.Empty;

    [ObservableProperty]
    private HtmlWebViewSource? _mapHtmlSource;

    [ObservableProperty]
    private bool _hasRelatedTours;

    public ObservableCollection<AudioGuide> AudioGuides { get; } = new();
    public ObservableCollection<RelatedTourItem> RelatedTours { get; } = new();

    public LocationDetailViewModel(INavigationService navigationService, IApiService apiService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
    }

    partial void OnLocationIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadLocationDetailsAsync(value);
        }
    }

    private async Task LoadLocationDetailsAsync(string locationId)
    {
        var location = await _apiService.GetLocationByIdAsync(locationId);

        if (location == null)
        {
            var locations = await _apiService.GetLocationsAsync();
            location = locations.ElementAtOrDefault(int.TryParse(locationId, out var idx) ? idx - 1 : -1);
        }

        if (location == null) return;

        LocationName = location.Name;
        Title = LocationName;
        Description = location.Description;
        ImageUrl = location.ImageUrl;
        Address = location.Address;
        Duration = location.Duration;

        // Category name
        var categories = await _apiService.GetCategoriesAsync();
        var cat = categories.FirstOrDefault(c => c.Id == location.CategoryId);
        CategoryName = cat?.Name ?? "Di tích";

        AudioGuides.Clear();
        foreach (var audio in location.AudioGuides)
        {
            AudioGuides.Add(audio);
        }
        AudioGuideCountText = $"{AudioGuides.Count} bài";

        // Map HTML
        BuildMapHtml(location.Latitude, location.Longitude, location.Name);

        // Related Tours
        var tours = await _apiService.GetToursAsync();
        RelatedTours.Clear();
        foreach (var tour in tours)
        {
            if (tour.LocationIds.Contains(location.Id))
            {
                RelatedTours.Add(new RelatedTourItem
                {
                    Id = tour.Id,
                    Name = tour.Name,
                    Info = $"{tour.LocationIds.Count} điểm · {tour.Duration} phút"
                });
            }
        }
        HasRelatedTours = RelatedTours.Count > 0;
    }

    private void BuildMapHtml(double lat, double lng, string name)
    {
        var latStr = lat.ToString(CultureInfo.InvariantCulture);
        var lngStr = lng.ToString(CultureInfo.InvariantCulture);

        var html = $@"<!DOCTYPE html>
<html><head>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
<style>html,body,#map{{height:100%;margin:0;padding:0}}</style>
</head><body>
<div id='map'></div>
<script>
var map=L.map('map').setView([{latStr},{lngStr}],16);
L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',{{
  maxZoom:19,attribution:'© OpenStreetMap'}}).addTo(map);
var icon=L.divIcon({{className:'',html:'<div style=""width:28px;height:28px;background:#512BD4;border-radius:50%;border:3px solid white;box-shadow:0 2px 6px rgba(0,0,0,.3)""></div>',iconSize:[28,28],iconAnchor:[14,14]}});
L.marker([{latStr},{lngStr}],{{icon:icon}}).addTo(map).bindPopup('{name.Replace("'", "\\'")}').openPopup();
</script></body></html>";

        MapHtmlSource = new HtmlWebViewSource { Html = html };
    }

    [RelayCommand]
    private async Task PlayAudioAsync(AudioGuide? audioGuide)
    {
        if (audioGuide is null)
            return;

        var audioSource = !string.IsNullOrWhiteSpace(audioGuide.CloudinaryAudioUrl)
            ? audioGuide.CloudinaryAudioUrl
            : audioGuide.AudioUrl;

        await _navigationService.NavigateToAsync(nameof(Views.AudioPlayerPage),
            new Dictionary<string, object>
            {
                { "LocationId", LocationId },
                { "AudioGuideId", audioGuide.Id },
                { "AudioUrl", audioSource }
            });
    }

    [RelayCommand]
    private async Task StartListeningAsync()
    {
        var firstAudio = AudioGuides.FirstOrDefault();
        if (firstAudio != null)
        {
            await PlayAudioAsync(firstAudio);
        }
    }

    [RelayCommand]
    private async Task OpenTourAsync(RelatedTourItem? tour)
    {
        if (tour is null) return;
        await _navigationService.NavigateToAsync(nameof(Views.TourDetailPage),
            new Dictionary<string, object> { { "TourId", tour.Id } });
    }
}
