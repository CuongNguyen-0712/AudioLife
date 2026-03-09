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

    [ObservableProperty]
    private string _totalSizeText = "0 MB";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<DownloadItem> Downloads { get; } = new();

    public DownloadsViewModel(IApiService apiService, IAudioService audioService, INavigationService navigationService)
    {
        _apiService = apiService;
        _audioService = audioService;
        _navigationService = navigationService;
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
                    Title = audio?.Title ?? "Unknown",
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
        await _audioService.PlayAsync(item.AudioGuideId);
    }

    [RelayCommand]
    private async Task DeleteDownloadAsync(DownloadItem? item)
    {
        if (item == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Xóa audio",
            $"Bạn muốn xóa \"{item.Title}\"?",
            "Xóa", "Hủy");

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
            "Xóa tất cả",
            "Bạn muốn xóa toàn bộ audio đã tải?",
            "Xóa tất cả", "Hủy");

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
    public string Title { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
    public int Duration { get; set; }
}
