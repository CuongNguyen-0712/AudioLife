using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private int _locationCount;

    [ObservableProperty]
    private int _audioCount;

    [ObservableProperty]
    private int _tourCount;

    public AboutViewModel()
    {
        var locations = Data.SampleData.GetLocations();
        var tours = Data.SampleData.GetTours();
        LocationCount = locations.Count;
        AudioCount = locations.Sum(l => l.AudioGuides.Count);
        TourCount = tours.Count;
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
