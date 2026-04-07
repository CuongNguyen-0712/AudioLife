using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _hasItems;

    [ObservableProperty]
    private Location? _selectedLocation;

    public ObservableCollection<Location> FavoriteLocations { get; } = new();

    public FavoritesViewModel(INavigationService navigationService, IApiService apiService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _ = LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        FavoriteLocations.Clear();
        var favorites = await _apiService.GetFavoriteLocationsAsync();

        foreach (var location in favorites)
        {
            FavoriteLocations.Add(location);
        }

        UpdateEmptyState();
    }


    private void UpdateEmptyState()
    {
        IsEmpty = FavoriteLocations.Count == 0;
        HasItems = FavoriteLocations.Count > 0;
    }

    [RelayCommand]
    private async Task LocationSelectedAsync()
    {
        if (SelectedLocation is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.LocationDetailPage),
            new Dictionary<string, object> { { "LocationId", SelectedLocation.Id } });

        SelectedLocation = null;
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(Location? location)
    {
        if (location is null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Xóa khỏi yêu thích",
            $"Bạn có chắc muốn xóa \"{location.Name}\" khỏi danh sách yêu thích?",
            "Xóa",
            "Hủy");

        if (confirm)
        {
            FavoriteLocations.Remove(location);
            UpdateEmptyState();
        }
    }

    [RelayCommand]
    private async Task ExploreAsync()
    {
        await _navigationService.NavigateToAsync("//MainPage");
    }
}
