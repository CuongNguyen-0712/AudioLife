using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasHistory = true;

    public ObservableCollection<HistoryGroup> HistoryGroups { get; } = new();

    public HistoryViewModel(IApiService apiService, INavigationService navigationService)
    {
        _apiService = apiService;
        _navigationService = navigationService;
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var history = await _apiService.GetListeningHistoryAsync();
            HasHistory = history.Count > 0;

            HistoryGroups.Clear();

            // Group by date
            var grouped = history.GroupBy(h => h.ListenedAt.Date)
                .OrderByDescending(g => g.Key);

            foreach (var group in grouped)
            {
                var label = group.Key == DateTime.Today ? "Hôm nay"
                    : group.Key == DateTime.Today.AddDays(-1) ? "Hôm qua"
                    : group.Key.ToString("dd/MM/yyyy");

                HistoryGroups.Add(new HistoryGroup(label, group.ToList()));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task HistoryItemTappedAsync(ListeningHistory? item)
    {
        if (item == null) return;

        await _navigationService.NavigateToAsync(nameof(Views.AudioPlayerPage),
            new Dictionary<string, object>
            {
                { "LocationId", item.LocationId },
                { "AudioUrl", item.AudioGuideId }
            });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadHistoryAsync();
    }
}

public class HistoryGroup : List<ListeningHistory>
{
    public string DateLabel { get; }

    public HistoryGroup(string dateLabel, List<ListeningHistory> items) : base(items)
    {
        DateLabel = dateLabel;
    }
}
