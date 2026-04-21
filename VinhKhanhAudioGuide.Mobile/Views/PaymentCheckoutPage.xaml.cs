using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentCheckoutPage : ContentPage
{
    private readonly PaymentPlanOption _plan;
    private readonly IApiService _apiService;
    private readonly IAppSessionStore _sessionStore;
    private readonly ILocalizationService _localizationService;
    private bool _isProcessingPayment;

    public PaymentCheckoutPage(PaymentPlanOption plan)
    {
        InitializeComponent();
        _plan = plan;
        _apiService = ResolveApiService();
        _sessionStore = ResolveSessionStore();
        _localizationService = ResolveLocalizationService();
        ApplyPlan();
    }

    private void ApplyPlan()
    {
        PlanTitleLabel.Text = _plan.Title;
        PlanSubtitleLabel.Text = _plan.PriceLabel;
        PriceLabel.Text = string.Format(_localizationService.GetString("PaymentCheckout_PriceAmountFormat"), _plan.Amount);
        PlanDescriptionLabel.Text = _plan.Description;
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        if (_isProcessingPayment)
        {
            return;
        }

        _isProcessingPayment = true;

        try
        {
            var payload = App.PendingQrPayload;
            var deviceId = await _sessionStore.GetOrCreateDeviceIdAsync();
            var deviceSession = await _apiService.CheckDeviceSessionAsync(deviceId);
            var sessionToken = string.IsNullOrWhiteSpace(deviceSession?.SessionToken)
                ? Guid.NewGuid().ToString("N")
                : deviceSession!.SessionToken;

            var packageId = string.IsNullOrWhiteSpace(_plan.Id)
                ? payload?.PaymentPackageId ?? string.Empty
                : _plan.Id;

            if (string.IsNullOrWhiteSpace(packageId))
            {
                await DisplayAlert(
                    _localizationService.GetString("PaymentCheckout_AlertMissingPackageTitle"),
                    _localizationService.GetString("PaymentCheckout_AlertMissingPackageMessage"),
                    _localizationService.GetString("Common_Close"));
                return;
            }

            var request = new PaymentCompletionRequest(
                deviceId,
                sessionToken,
                deviceSession?.RefreshToken,
                !string.IsNullOrWhiteSpace(payload?.IdentityToken) ? payload!.IdentityToken : null,
                deviceSession?.UserAppId,
                payload?.LocationId,
                payload?.AudioGuideId,
                payload?.AudioUrl,
                packageId,
                "Paid",
                $"pay_{DateTime.UtcNow:yyyyMMddHHmmss}");

            var result = await _apiService.CompletePaymentAsync(request);
            if (result is null || !result.Success)
            {
                await DisplayAlert(
                    _localizationService.GetString("PaymentCheckout_AlertFailedTitle"),
                    result?.Message ?? _localizationService.GetString("PaymentCheckout_AlertFailedMessage"),
                    _localizationService.GetString("Common_Close"));
                return;
            }

            var validation = await _apiService.ValidateSessionAsync(result.SessionToken, deviceId);
            if (validation is null || !validation.IsValid)
            {
                await DisplayAlert(
                    _localizationService.GetString("PaymentCheckout_AlertPendingTitle"),
                    validation?.Message ?? _localizationService.GetString("PaymentCheckout_AlertPendingMessage"),
                    _localizationService.GetString("Common_Close"));
                return;
            }

            App.ClearPendingQrPayload();

            await DisplayAlert(
                _localizationService.GetString("PaymentCheckout_AlertSuccessTitle"),
                string.Format(_localizationService.GetString("PaymentCheckout_AlertSuccessMessageFormat"), _plan.Title),
                _localizationService.GetString("Common_Close"));
            App.NavigateToLanguageSelection();
        }
        finally
        {
            _isProcessingPayment = false;
        }
    }

    private static IApiService ResolveApiService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IApiService>()
            ?? new ApiService(new LocalDatabaseService(), new LocalizationService());
    }

    private static IAppSessionStore ResolveSessionStore()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IAppSessionStore>()
            ?? new AppSessionStore();
    }

    private static ILocalizationService ResolveLocalizationService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
            ?? new LocalizationService();
    }
}
