using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedLanguage = "Tiếng Việt";

    [ObservableProperty]
    private bool _autoPlayNext = true;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private int _skipInterval = 10;

    [ObservableProperty]
    private bool _downloadOnWifiOnly = true;

    [ObservableProperty]
    private string _audioQuality = "Cao (320kbps)";

    [ObservableProperty]
    private string _storageUsed = "45.2 MB";

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableLocationNotifications = true;

    [ObservableProperty]
    private string _themeMode = "Theo hệ thống";

    public SettingsViewModel()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load settings from preferences
        AutoPlayNext = Preferences.Get("AutoPlayNext", true);
        PlaybackSpeed = Preferences.Get("PlaybackSpeed", 1.0);
        SkipInterval = Preferences.Get("SkipInterval", 10);
        DownloadOnWifiOnly = Preferences.Get("DownloadOnWifiOnly", true);
        EnableNotifications = Preferences.Get("EnableNotifications", true);
        EnableLocationNotifications = Preferences.Get("EnableLocationNotifications", true);
    }

    partial void OnAutoPlayNextChanged(bool value)
    {
        Preferences.Set("AutoPlayNext", value);
    }

    partial void OnDownloadOnWifiOnlyChanged(bool value)
    {
        Preferences.Set("DownloadOnWifiOnly", value);
    }

    partial void OnEnableNotificationsChanged(bool value)
    {
        Preferences.Set("EnableNotifications", value);
    }

    partial void OnEnableLocationNotificationsChanged(bool value)
    {
        Preferences.Set("EnableLocationNotifications", value);
    }

    [RelayCommand]
    private async Task SelectLanguageAsync()
    {
        var languages = new[] { "Tiếng Việt", "English", "日本語", "한국어", "中文" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            "Chọn ngôn ngữ", "Hủy", null, languages);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            SelectedLanguage = result;
            Preferences.Set("Language", result);
        }
    }

    [RelayCommand]
    private async Task SelectPlaybackSpeedAsync()
    {
        var speeds = new[] { "0.5x", "0.75x", "1.0x", "1.25x", "1.5x", "2.0x" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            "Tốc độ phát", "Hủy", null, speeds);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            PlaybackSpeed = double.Parse(result.Replace("x", ""));
            Preferences.Set("PlaybackSpeed", PlaybackSpeed);
        }
    }

    [RelayCommand]
    private async Task SelectSkipIntervalAsync()
    {
        var intervals = new[] { "5 giây", "10 giây", "15 giây", "30 giây" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            "Khoảng tua", "Hủy", null, intervals);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            SkipInterval = int.Parse(result.Replace(" giây", ""));
            Preferences.Set("SkipInterval", SkipInterval);
        }
    }

    [RelayCommand]
    private async Task SelectAudioQualityAsync()
    {
        var qualities = new[] { "Thấp (64kbps)", "Trung bình (128kbps)", "Cao (320kbps)" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            "Chất lượng Audio", "Hủy", null, qualities);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            AudioQuality = result;
            Preferences.Set("AudioQuality", result);
        }
    }

    [RelayCommand]
    private async Task ClearDownloadsAsync()
    {
        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Xóa audio đã tải",
            "Bạn có chắc muốn xóa tất cả audio đã tải?",
            "Xóa",
            "Hủy");

        if (confirm)
        {
            // Clear downloaded files
            StorageUsed = "0 MB";
        }
    }

    [RelayCommand]
    private async Task SelectThemeAsync()
    {
        var themes = new[] { "Sáng", "Tối", "Theo hệ thống" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            "Chế độ giao diện", "Hủy", null, themes);

        if (!string.IsNullOrEmpty(result) && result != "Hủy")
        {
            ThemeMode = result;
            Preferences.Set("ThemeMode", result);
            
            Application.Current.UserAppTheme = result switch
            {
                "Sáng" => AppTheme.Light,
                "Tối" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}
