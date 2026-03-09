using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class ToursViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isRefreshing;

    public ObservableCollection<TourDisplayModel> FeaturedTours { get; } = new();
    public ObservableCollection<TourDisplayModel> AllTours { get; } = new();

    public ToursViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        LoadTours();
    }

    private void LoadTours()
    {
        var tours = Data.SampleData.GetTours();

        foreach (var tour in tours)
        {
            var displayModel = new TourDisplayModel
            {
                Id = tour.Id,
                Name = tour.Name,
                Description = tour.Description,
                ImageUrl = tour.ImageUrl,
                Duration = tour.Duration,
                DurationText = FormatDuration(tour.Duration),
                LocationCount = tour.LocationIds.Count,
                IsFeatured = tour.IsFeatured,
                PriceText = tour.Price <= 0 ? "Miễn phí" : $"{tour.Price:N0} VNĐ"
            };

            if (tour.IsFeatured)
            {
                FeaturedTours.Add(displayModel);
            }
            AllTours.Add(displayModel);
        }
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
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await Task.Delay(1000);
        FeaturedTours.Clear();
        AllTours.Clear();
        LoadTours();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task TourTappedAsync(TourDisplayModel? tour)
    {
        if (tour is null) return;

        await _navigationService.NavigateToAsync(nameof(Views.TourDetailPage),
            new Dictionary<string, object> { { "TourId", tour.Id } });
    }
}

public class TourDisplayModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string DurationText { get; set; } = string.Empty;
    public int LocationCount { get; set; }
    public bool IsFeatured { get; set; }
    public string PriceText { get; set; } = string.Empty;
}
