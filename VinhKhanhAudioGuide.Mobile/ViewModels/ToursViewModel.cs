using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class ToursViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IApiService _apiService;
    private readonly ITourCheckpointService _tourCheckpointService;
    private readonly ILocalizationService _localizationService;
    private bool _resumePromptHandled;
    private string _lastPromptedCheckpoint = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    public ObservableCollection<TourDisplayModel> FeaturedTours { get; } = new();
    public ObservableCollection<TourDisplayModel> AllTours { get; } = new();

    public ToursViewModel(
        INavigationService navigationService,
        IApiService apiService,
        ITourCheckpointService tourCheckpointService,
        ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _apiService = apiService;
        _tourCheckpointService = tourCheckpointService;
        _localizationService = localizationService;
        _ = LoadToursAsync();
    }

    public async Task OnAppearingAsync()
    {
        var checkpoint = await _tourCheckpointService.GetAsync();
        if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.TourId))
        {
            _resumePromptHandled = false;
            _lastPromptedCheckpoint = string.Empty;
            return;
        }

        var checkpointKey = $"{checkpoint.TourId}|{checkpoint.LocationId}|{checkpoint.AudioGuideId}|{checkpoint.AudioPositionSeconds:0.##}";
        if (_resumePromptHandled && string.Equals(_lastPromptedCheckpoint, checkpointKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _resumePromptHandled = true;
        _lastPromptedCheckpoint = checkpointKey;

        var savedAt = checkpoint.SavedAtUtc == default
            ? DateTime.Now
            : checkpoint.SavedAtUtc.ToLocalTime();

        var resume = await MainThread.InvokeOnMainThreadAsync(async () =>
            await Application.Current!.MainPage!.DisplayAlert(
                "Tiếp tục lộ trình?",
                $"Bạn đã tạm dừng tại {checkpoint.LocationName} lúc {savedAt:HH:mm, dd/MM}. Tiếp tục từ điểm dừng trước?",
                "Tiếp tục",
                "Để sau"));

        if (!resume)
        {
            return;
        }

        await _tourCheckpointService.ClearAsync();

        await _navigationService.NavigateToAsync("///MapPage",
            new Dictionary<string, object>
            {
                { "TourId", checkpoint.TourId },
                { "ResumeLocationId", checkpoint.LocationId },
                { "ResumeAudioGuideId", checkpoint.AudioGuideId },
                { "ResumeAudioUrl", checkpoint.AudioUrl },
                { "ResumePositionSeconds", checkpoint.AudioPositionSeconds },
                { "ResumeSessionId", Guid.NewGuid().ToString("N") }
            });
    }

    private async Task LoadToursAsync()
    {
        FeaturedTours.Clear();
        AllTours.Clear();

        var tours = await _apiService.GetToursAsync();

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

    private string FormatDuration(int minutes)
    {
        if (minutes <= 0)
        {
            return string.Format(_localizationService.GetString("Tour_DurationMinutesFormat"), 0);
        }

        if (minutes < 60)
        {
            return string.Format(_localizationService.GetString("Tour_DurationMinutesFormat"), minutes);
        }

        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0
            ? string.Format(_localizationService.GetString("Tour_DurationHoursMinutesFormat"), hours, mins)
            : string.Format(_localizationService.GetString("Tour_DurationHoursOnlyFormat"), hours);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadToursAsync();
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
