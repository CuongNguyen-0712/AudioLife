using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class DownloadsViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IAudioService _audioService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string _totalSizeText = "0 MB";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<DownloadItem> Downloads { get; } = new();

    public DownloadsViewModel(
        IApiService apiService,
        IAudioService audioService,
        INavigationService navigationService,
        ILocalizationService localizationService)
    {
        _apiService = apiService;
        _audioService = audioService;
        _navigationService = navigationService;
        _localizationService = localizationService;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var downloads = await _apiService.GetDownloadedAudiosAsync();
            var totalSize = await _apiService.GetTotalDownloadSizeAsync();

            TotalSizeText = FormatSize(totalSize);
            Downloads.Clear();

            foreach (var dl in downloads)
            {
                var audio = await _apiService.GetAudioGuideByIdAsync(dl.AudioGuideId);
                Downloads.Add(new DownloadItem
                {
                    AudioGuideId = dl.AudioGuideId,
                    LocalPath = dl.LocalPath,
                    Title = audio?.Title ?? _localizationService.GetString("Common_Unknown"),
                    LocationName = audio?.LocationId != null
                        ? (await _apiService.GetLocationByIdAsync(audio.LocationId))?.Name ?? ""
                        : "",
                    FileSize = FormatSize(dl.FileSize),
                    DownloadedAt = dl.DownloadedAt,
                    Duration = audio?.Duration ?? 0
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PlayDownloadAsync(DownloadItem? item)
    {
        if (item == null) return;
        var source = string.IsNullOrWhiteSpace(item.LocalPath) ? item.AudioGuideId : item.LocalPath;
        await _audioService.PlayAsync(source);
    }

    [RelayCommand]
    private async Task DeleteDownloadAsync(DownloadItem? item)
    {
        if (item == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            _localizationService.GetString("Downloads_DeleteTitle"),
            string.Format(_localizationService.GetString("Downloads_DeleteMessageFormat"), item.Title),
            _localizationService.GetString("Common_Delete"),
            _localizationService.GetString("Common_Cancel"));

        if (confirm)
        {
            await _apiService.DeleteDownloadedAudioAsync(item.AudioGuideId);
            Downloads.Remove(item);
            var totalSize = await _apiService.GetTotalDownloadSizeAsync();
            TotalSizeText = FormatSize(totalSize);
        }
    }

    [RelayCommand]
    private async Task DeleteAllAsync()
    {
        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            _localizationService.GetString("Downloads_DeleteAllTitle"),
            _localizationService.GetString("Downloads_DeleteAllMessage"),
            _localizationService.GetString("Downloads_DeleteAll"),
            _localizationService.GetString("Common_Cancel"));

        if (confirm)
        {
            foreach (var dl in Downloads.ToList())
            {
                await _apiService.DeleteDownloadedAudioAsync(dl.AudioGuideId);
            }
            Downloads.Clear();
            TotalSizeText = "0 MB";
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}

public class DownloadItem
{
    public string AudioGuideId { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
    public int Duration { get; set; }
}
