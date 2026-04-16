using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class AboutViewModel : LoadStateViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private int _locationCount;

    [ObservableProperty]
    private int _audioCount;

    [ObservableProperty]
    private int _tourCount;

    private bool _hasLoaded;

    public AboutViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task OnAppearingAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        BeginLoading();
        try
        {
            var locationsTask = _apiService.GetLocationsAsync();
            var toursTask = _apiService.GetToursAsync();
            await Task.WhenAll(locationsTask, toursTask);

            var locations = locationsTask.Result;
            var tours = toursTask.Result;
            LocationCount = locations.Count;
            AudioCount = locations.Sum(l => l.AudioGuides.Count);
            TourCount = tours.Count;

            _hasLoaded = true;
            CompleteLoading(true);
        }
        catch (Exception ex)
        {
            FailLoading(ex.Message);
        }
    }

    [RelayCommand]
    private async Task RateAppAsync()
    {
        try
        {
            // Open app store rating page
            await Browser.Default.OpenAsync(
                "https://play.google.com/store/apps/details?id=com.vinhkhanh.audioguide",
                BrowserLaunchMode.External);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Thông báo", "Không thể mở cửa hàng ứng dụng.", "OK");
        }
    }

    [RelayCommand]
    private async Task ShareAppAsync()
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Vinh Khanh Audio Guide",
            Text = "Khám phá văn hóa và lịch sử Việt Nam cùng Vinh Khanh Audio Guide! Tải ngay:",
            Uri = "https://vinhkhanhaudioguide.com"
        });
    }

    [RelayCommand]
    private async Task OpenTermsAsync()
    {
        try
        {
            await Browser.Default.OpenAsync("https://vinhkhanhaudioguide.com/terms", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể mở trình duyệt.", "OK");
        }
    }

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync()
    {
        try
        {
            await Browser.Default.OpenAsync("https://vinhkhanhaudioguide.com/privacy", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                "Lỗi", "Không thể mở trình duyệt.", "OK");
        }
    }
}

public class FeatureItem
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
