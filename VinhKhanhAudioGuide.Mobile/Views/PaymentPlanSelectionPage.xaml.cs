using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentPlanSelectionPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly PaymentPlanOption _fallbackDailyPlan = new("daily", "10.000đ/ngày", "Một ngày sử dụng", "Phù hợp khi bạn muốn trải nghiệm nhanh trong một ngày, tối ưu cho khách ghé ngắn.", 10000m);
    private readonly PaymentPlanOption _fallbackFullTourPlan = new("full-tour", "29.000đ/full tour", "Một lần thanh toán", "Mở khóa toàn bộ tour, phù hợp khi bạn muốn nghe trọn vẹn nội dung đã quét.", 29000m);

    private PaymentPlanOption _dailyPlan;
    private PaymentPlanOption _fullTourPlan;
    private PaymentPlanOption _selectedPlan;
    private bool _hasLoadedPackages;
    private bool _hasSyncedPackagesFromServer;

    public PaymentPlanSelectionPage()
    {
        InitializeComponent();
        _apiService = ResolveApiService();
        _dailyPlan = _fallbackDailyPlan;
        _fullTourPlan = _fallbackFullTourPlan;
        _selectedPlan = _dailyPlan;
        _hasSyncedPackagesFromServer = false;
        ApplySelectionState();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadPackagesAsync();
        ApplySelectionState();
    }

    private async Task LoadPackagesAsync()
    {
        if (_hasLoadedPackages)
        {
            return;
        }

        _hasLoadedPackages = true;

        try
        {
            var packages = await _apiService.GetPaymentPackagesAsync();
            if (packages.Count == 0)
            {
                _hasSyncedPackagesFromServer = false;
                SelectionSummaryLabel.Text = "Không tải được gói từ máy chủ. Vui lòng kiểm tra mạng.";
                ContinueButton.IsEnabled = false;
                return;
            }

            _dailyPlan = ToPlanOption(packages.FirstOrDefault(item => string.Equals(item.Id, _fallbackDailyPlan.Id, StringComparison.OrdinalIgnoreCase)), _fallbackDailyPlan);
            _fullTourPlan = ToPlanOption(packages.FirstOrDefault(item => string.Equals(item.Id, _fallbackFullTourPlan.Id, StringComparison.OrdinalIgnoreCase)), _fallbackFullTourPlan);
            _selectedPlan = _dailyPlan;
            _hasSyncedPackagesFromServer = true;
            ApplySelectionState();
        }
        catch
        {
            _hasSyncedPackagesFromServer = false;
            SelectionSummaryLabel.Text = "Không kết nối được máy chủ gói thanh toán. Vui lòng thử lại.";
            ContinueButton.IsEnabled = false;
        }
    }

    private void OnDailyPlanTapped(object sender, TappedEventArgs e)
    {
        _selectedPlan = _dailyPlan;
        ApplySelectionState();
    }

    private void OnFullTourPlanTapped(object sender, TappedEventArgs e)
    {
        _selectedPlan = _fullTourPlan;
        ApplySelectionState();
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        if (!_hasSyncedPackagesFromServer)
        {
            await DisplayAlert("Chưa đồng bộ gói", "Cần tải gói thanh toán từ máy chủ trước khi tiếp tục.", "Đóng");
            return;
        }

        await Navigation.PushAsync(new PaymentCheckoutPage(_selectedPlan));
    }

    private static PaymentPlanOption ToPlanOption(PaymentPackage? package, PaymentPlanOption fallback)
    {
        if (package is null)
        {
            return fallback;
        }

        var priceLabel = package.DurationDays <= 1 ? "Một ngày sử dụng" : "Một lần thanh toán";
        return new PaymentPlanOption(package.Id, package.Name, priceLabel, package.Description ?? fallback.Description, package.Price);
    }

    private void ApplySelectionState()
    {
        SetSelectedState(DailyPlanCard, DailyPlanIndicator, DailyPlanIndicatorLabel, _selectedPlan.Id == _dailyPlan.Id);
        SetSelectedState(FullTourPlanCard, FullTourPlanIndicator, FullTourPlanIndicatorLabel, _selectedPlan.Id == _fullTourPlan.Id);
        SelectionSummaryLabel.Text = $"Đang chọn: {_selectedPlan.Title}";
        ContinueButton.IsEnabled = _hasSyncedPackagesFromServer;
    }

    private static void SetSelectedState(Border card, Border indicator, Label indicatorLabel, bool isSelected)
    {
        var resources = Application.Current!.Resources;
        card.BackgroundColor = isSelected ? (Color)resources["SurfaceContainerLow"] : (Color)resources["SurfaceContainerLowest"];
        indicator.BackgroundColor = isSelected ? (Color)resources["Primary"] : (Color)resources["SurfaceContainerHigh"];
        indicatorLabel.Text = isSelected ? "✓" : string.Empty;
        indicatorLabel.TextColor = (Color)resources["OnPrimary"];
        card.StrokeThickness = isSelected ? 2 : 0;
        card.Stroke = isSelected ? (Color)resources["PrimaryContainer"] : Colors.Transparent;
    }

    private static IApiService ResolveApiService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IApiService>()
            ?? new ApiService(new LocalDatabaseService(), new LocalizationService());
    }
}
