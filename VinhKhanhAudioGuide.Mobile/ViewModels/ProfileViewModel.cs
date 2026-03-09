using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _userName = "Nguyễn Văn A";

    [ObservableProperty]
    private string _email = "nguyenvana@email.com";

    [ObservableProperty]
    private string _avatarUrl = "default_avatar";

    [ObservableProperty]
    private int _visitedCount = 5;

    [ObservableProperty]
    private int _favoritesCount = 8;

    [ObservableProperty]
    private int _downloadedCount = 12;

    public ProfileViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        LoadUserProfile();
    }

    private void LoadUserProfile()
    {
        // Load from local storage or API
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.EditProfilePage));
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
    private async Task GoToSettingsAsync()
    {
        await _navigationService.NavigateToAsync(nameof(Views.SettingsPage));
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

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Application.Current!.MainPage!.DisplayAlert(
            "Đăng xuất",
            "Bạn có chắc muốn đăng xuất?",
            "Đăng xuất",
            "Hủy");

        if (confirm)
        {
            // Clear user data and navigate to home
            Preferences.Default.Clear();
            await _navigationService.NavigateToAsync("//MainPage");
        }
    }
}
