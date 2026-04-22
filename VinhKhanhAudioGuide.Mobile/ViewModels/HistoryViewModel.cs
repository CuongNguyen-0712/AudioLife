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
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasHistory = true;

    public ObservableCollection<HistoryGroup> HistoryGroups { get; } = new();

    public HistoryViewModel(
        IApiService apiService,
        INavigationService navigationService,
        ILocalizationService localizationService)
    {
        _apiService = apiService;
        _navigationService = navigationService;
        _localizationService = localizationService;
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        try
        {
            var history = await _apiService.GetListeningHistoryAsync();
            await EnrichHistoryLanguageAsync(history);
            HasHistory = history.Count > 0;

            HistoryGroups.Clear();

            // Group by date
            var grouped = history.GroupBy(h => h.ListenedAt.Date)
                .OrderByDescending(g => g.Key);

            foreach (var group in grouped)
            {
                var label = group.Key == DateTime.Today ? _localizationService.GetString("History_Today")
                    : group.Key == DateTime.Today.AddDays(-1) ? _localizationService.GetString("History_Yesterday")
                    : group.Key.ToString("dd/MM/yyyy");

                HistoryGroups.Add(new HistoryGroup(label, group.ToList()));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EnrichHistoryLanguageAsync(List<ListeningHistory> history)
    {
        var missingLanguageItems = history
            .Where(item => string.IsNullOrWhiteSpace(item.Language) && !string.IsNullOrWhiteSpace(item.AudioGuideId))
            .ToList();

        if (missingLanguageItems.Count == 0)
        {
            return;
        }

        var tasks = missingLanguageItems.Select(async item =>
        {
            try
            {
                var audioGuide = await _apiService.GetAudioGuideByIdAsync(item.AudioGuideId);
                if (!string.IsNullOrWhiteSpace(audioGuide?.Language))
                {
                    item.Language = audioGuide.Language;
                }
            }
            catch
            {
                // Keep default display language fallback when metadata fetch fails.
            }
        });

        await Task.WhenAll(tasks);
    }

    [RelayCommand]
    private async Task HistoryItemTappedAsync(ListeningHistory? item)
    {
        if (item == null) return;

        await _navigationService.NavigateToAsync(nameof(Views.AudioPlayerPage),
            new Dictionary<string, object>
            {
                { "LocationId", item.LocationId },
                { "AudioGuideId", item.AudioGuideId }
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
