using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;

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

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Location> FeaturedLocations { get; } = new();
    public ObservableCollection<Location> MoreLocations { get; } = new();
    public ObservableCollection<Location> FavoriteLocations { get; } = new();
    public ObservableCollection<FeaturedTourItem> FeaturedTours { get; } = new();

    public MainViewModel(INavigationService navigationService, IApiService apiService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
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
