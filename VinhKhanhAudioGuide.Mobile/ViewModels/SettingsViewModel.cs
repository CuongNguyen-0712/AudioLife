using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private bool _autoNearestPoiPlayback = true;

    [ObservableProperty]
    private string _locationScanInterval = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = string.Empty;

    [ObservableProperty]
    private bool _autoPlayNext = true;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private int _skipInterval = 10;

    [ObservableProperty]
    private bool _downloadOnWifiOnly = true;

    [ObservableProperty]
    private string _audioQuality = string.Empty;

    [ObservableProperty]
    private string _storageUsed = "45.2 MB";

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableLocationNotifications = true;

    [ObservableProperty]
    private string _themeMode = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    private bool _hasLoaded;

    public SettingsViewModel(INavigationService navigationService, ILocalizationService localizationService)
    {
        _navigationService = navigationService;
        _localizationService = localizationService;

        _localizationService.CultureChanged += OnCultureChanged;
    }

    private void BeginLoading()
    {
        IsError = false;
        ErrorMessage = string.Empty;
        IsLoading = true;
    }

    private void CompleteLoading(bool hasData)
    {
        HasData = hasData;
        IsEmpty = !hasData;
        IsError = false;
        ErrorMessage = string.Empty;
        IsLoading = false;
    }

    private void FailLoading(string errorMessage)
    {
        HasData = false;
        IsEmpty = true;
        IsError = true;
        ErrorMessage = errorMessage;
        IsLoading = false;
    }

    public Task OnAppearingAsync()
    {
        if (_hasLoaded)
        {
            return Task.CompletedTask;
        }

        BeginLoading();
        try
        {
            LoadSettings();
            _hasLoaded = true;
            CompleteLoading(true);
        }
        catch (Exception ex)
        {
            FailLoading(ex.Message);
        }

        return Task.CompletedTask;
    }

    private void LoadSettings()
    {
        // Load settings from preferences
        AutoNearestPoiPlayback = Preferences.Get("AutoNearestPoiPlayback", true);
        AutoPlayNext = Preferences.Get("AutoPlayNext", true);
        PlaybackSpeed = Preferences.Get("PlaybackSpeed", 1.0);
        SkipInterval = Preferences.Get("SkipInterval", 10);
        DownloadOnWifiOnly = Preferences.Get("DownloadOnWifiOnly", true);
        EnableNotifications = Preferences.Get("EnableNotifications", true);
        EnableLocationNotifications = Preferences.Get("EnableLocationNotifications", true);
        SelectedLanguage = _localizationService.GetCurrentLanguageDisplayName();
        LocationScanInterval = _localizationService.GetString("Settings_ScanEveryMinute");

        var qualityValue = Preferences.Get("AudioQuality", "high");
        AudioQuality = ToAudioQualityLabel(qualityValue);

        var themeValue = Preferences.Get("ThemeMode", "system");
        ThemeMode = ToThemeLabel(themeValue);
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SelectedLanguage = _localizationService.GetCurrentLanguageDisplayName();
        });
    }

    partial void OnAutoNearestPoiPlaybackChanged(bool value)
    {
        Preferences.Set("AutoNearestPoiPlayback", value);
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
        var supportedLanguages = _localizationService.GetSupportedLanguages();
        var options = supportedLanguages.Select(language => language.DisplayName).ToArray();
        var cancel = _localizationService.GetString("Common_Cancel");
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            _localizationService.GetString("Settings_SelectLanguage_Title"), cancel, null, options);

        if (!string.IsNullOrEmpty(result) && !string.Equals(result, cancel, StringComparison.Ordinal))
        {
            var selectedLanguage = supportedLanguages.FirstOrDefault(language =>
                string.Equals(language.DisplayName, result, StringComparison.Ordinal));

            if (selectedLanguage is not null)
            {
                _localizationService.SetCulture(selectedLanguage.CultureName);
                SelectedLanguage = selectedLanguage.DisplayName;
            }
        }
    }

    [RelayCommand]
    private async Task SelectPlaybackSpeedAsync()
    {
        var cancel = _localizationService.GetString("Common_Cancel");
        var speeds = new[] { "0.5x", "0.75x", "1.0x", "1.25x", "1.5x", "2.0x" };
        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            _localizationService.GetString("Settings_SelectPlaybackSpeed_Title"), cancel, null, speeds);

        if (!string.IsNullOrEmpty(result) && !string.Equals(result, cancel, StringComparison.Ordinal))
        {
            PlaybackSpeed = double.Parse(result.Replace("x", ""));
            Preferences.Set("PlaybackSpeed", PlaybackSpeed);
        }
    }

    [RelayCommand]
    private async Task SelectSkipIntervalAsync()
    {
        var cancel = _localizationService.GetString("Common_Cancel");
        var intervals = new[]
        {
            new { Label = string.Format(_localizationService.GetString("Settings_SkipOption_Seconds"), 5), Value = 5 },
            new { Label = string.Format(_localizationService.GetString("Settings_SkipOption_Seconds"), 10), Value = 10 },
            new { Label = string.Format(_localizationService.GetString("Settings_SkipOption_Seconds"), 15), Value = 15 },
            new { Label = string.Format(_localizationService.GetString("Settings_SkipOption_Seconds"), 30), Value = 30 }
        };

        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            _localizationService.GetString("Settings_SelectSkipInterval_Title"),
            cancel,
            null,
            intervals.Select(item => item.Label).ToArray());

        if (!string.IsNullOrEmpty(result) && !string.Equals(result, cancel, StringComparison.Ordinal))
        {
            var selected = intervals.FirstOrDefault(item => string.Equals(item.Label, result, StringComparison.Ordinal));
            if (selected is not null)
            {
                SkipInterval = selected.Value;
                Preferences.Set("SkipInterval", SkipInterval);
            }
        }
    }

    [RelayCommand]
    private async Task SelectAudioQualityAsync()
    {
        var cancel = _localizationService.GetString("Common_Cancel");
        var qualities = new[]
        {
            new { Label = _localizationService.GetString("Settings_AudioQuality_Low"), Value = "low" },
            new { Label = _localizationService.GetString("Settings_AudioQuality_Medium"), Value = "medium" },
            new { Label = _localizationService.GetString("Settings_AudioQuality_High"), Value = "high" }
        };

        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            _localizationService.GetString("Settings_SelectAudioQuality_Title"),
            cancel,
            null,
            qualities.Select(item => item.Label).ToArray());

        if (!string.IsNullOrEmpty(result) && !string.Equals(result, cancel, StringComparison.Ordinal))
        {
            var selected = qualities.FirstOrDefault(item => string.Equals(item.Label, result, StringComparison.Ordinal));
            if (selected is not null)
            {
                AudioQuality = selected.Label;
                Preferences.Set("AudioQuality", selected.Value);
            }
        }
    }

    [RelayCommand]
    private async Task ClearDownloadsAsync()
    {
        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            _localizationService.GetString("Settings_ClearDownloads_Title"),
            _localizationService.GetString("Settings_ClearDownloads_Confirm"),
            _localizationService.GetString("Common_Delete"),
            _localizationService.GetString("Common_Cancel"));

        if (confirm)
        {
            // Clear downloaded files
            StorageUsed = "0 MB";
        }
    }

    [RelayCommand]
    private async Task SelectThemeAsync()
    {
        var cancel = _localizationService.GetString("Common_Cancel");
        var themes = new[]
        {
            new { Label = _localizationService.GetString("Settings_Theme_Light"), Value = "light" },
            new { Label = _localizationService.GetString("Settings_Theme_Dark"), Value = "dark" },
            new { Label = _localizationService.GetString("Settings_Theme_System"), Value = "system" }
        };

        var result = await Application.Current!.MainPage!.DisplayActionSheet(
            _localizationService.GetString("Settings_SelectTheme_Title"),
            cancel,
            null,
            themes.Select(item => item.Label).ToArray());

        if (!string.IsNullOrEmpty(result) && !string.Equals(result, cancel, StringComparison.Ordinal))
        {
            var selected = themes.FirstOrDefault(item => string.Equals(item.Label, result, StringComparison.Ordinal));
            if (selected is null)
            {
                return;
            }

            ThemeMode = selected.Label;
            Preferences.Set("ThemeMode", selected.Value);
            
            Application.Current.UserAppTheme = selected.Value switch
            {
                "light" => AppTheme.Light,
                "dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }

    private string ToAudioQualityLabel(string quality)
    {
        return quality switch
        {
            "low" => _localizationService.GetString("Settings_AudioQuality_Low"),
            "medium" => _localizationService.GetString("Settings_AudioQuality_Medium"),
            _ => _localizationService.GetString("Settings_AudioQuality_High")
        };
    }

    private string ToThemeLabel(string theme)
    {
        return theme switch
        {
            "light" => _localizationService.GetString("Settings_Theme_Light"),
            "dark" => _localizationService.GetString("Settings_Theme_Dark"),
            _ => _localizationService.GetString("Settings_Theme_System")
        };
    }

    [RelayCommand]
    private async Task GoToFavoritesAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.FavoritesPage));
    }

    [RelayCommand]
    private async Task GoToDownloadsAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.DownloadsPage));
    }

    [RelayCommand]
    private async Task GoToHistoryAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.HistoryPage));
    }

    [RelayCommand]
    private async Task GoToHelpAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.HelpPage));
    }

    [RelayCommand]
    private async Task GoToAboutAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.AboutPage));
    }

}
