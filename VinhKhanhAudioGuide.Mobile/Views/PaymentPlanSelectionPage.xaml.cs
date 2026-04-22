using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentPlanSelectionPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ILocalizationService _localizationService;
    private readonly PaymentPlanOption _fallbackDailyPlan;
    private readonly PaymentPlanOption _fallbackFullTourPlan;

    private PaymentPlanOption _dailyPlan;
    private PaymentPlanOption _fullTourPlan;
    private PaymentPlanOption _selectedPlan;
    private bool _hasLoadedPackages;
    private bool _hasSyncedPackagesFromServer;

    public PaymentPlanSelectionPage()
    {
        InitializeComponent();
        _apiService = ResolveApiService();
        _localizationService = ResolveLocalizationService();
        _fallbackDailyPlan = new PaymentPlanOption(
            "daily",
            _localizationService.GetString("PaymentPlan_DailyTitle"),
            _localizationService.GetString("PaymentPlan_DailyPriceLabel"),
            _localizationService.GetString("PaymentPlan_DailyDescription"),
            10000m);
        _fallbackFullTourPlan = new PaymentPlanOption(
            "full-tour",
            _localizationService.GetString("PaymentPlan_FullTitle"),
            _localizationService.GetString("PaymentPlan_FullPriceLabel"),
            _localizationService.GetString("PaymentPlan_FullDescription"),
            29000m);
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
                UseFallbackPackages(_localizationService.GetString("PaymentPlan_StatusFallbackUsed"));
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
            UseFallbackPackages(_localizationService.GetString("PaymentPlan_StatusServerUnavailable"));
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
            await DisplayAlert(
                _localizationService.GetString("PaymentPlan_AlertNotSyncedTitle"),
                _localizationService.GetString("PaymentPlan_AlertNotSyncedMessage"),
                _localizationService.GetString("Common_Close"));
            return;
        }

        await Navigation.PushAsync(new PaymentCheckoutPage(_selectedPlan));
    }

    private PaymentPlanOption ToPlanOption(PaymentPackage? package, PaymentPlanOption fallback)
    {
        if (package is null)
        {
            return fallback;
        }

        var priceLabel = package.DurationDays <= 1
            ? _localizationService.GetString("PaymentPlan_DailyPriceLabel")
            : _localizationService.GetString("PaymentPlan_FullPriceLabel");
        return new PaymentPlanOption(package.Id, package.Name, priceLabel, package.Description ?? fallback.Description, package.Price);
    }

    private void ApplySelectionState()
    {
        SetSelectedState(DailyPlanCard, DailyPlanIndicator, DailyPlanIndicatorLabel, _selectedPlan.Id == _dailyPlan.Id);
        SetSelectedState(FullTourPlanCard, FullTourPlanIndicator, FullTourPlanIndicatorLabel, _selectedPlan.Id == _fullTourPlan.Id);
        SelectionSummaryLabel.Text = string.Format(
            _localizationService.GetString("PaymentPlan_SelectedSummaryFormat"),
            _selectedPlan.Title);
        ContinueButton.IsEnabled = _hasSyncedPackagesFromServer;
    }

    private void UseFallbackPackages(string statusMessage)
    {
        _dailyPlan = _fallbackDailyPlan;
        _fullTourPlan = _fallbackFullTourPlan;
        _selectedPlan = _dailyPlan;
        _hasSyncedPackagesFromServer = true;
        ApplySelectionState();
        SelectionSummaryLabel.Text = statusMessage;
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

    private static ILocalizationService ResolveLocalizationService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
            ?? new LocalizationService();
    }
}
