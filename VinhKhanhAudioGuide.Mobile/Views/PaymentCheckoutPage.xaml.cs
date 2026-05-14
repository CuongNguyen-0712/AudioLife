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
        // Bind thông tin gói thanh toán lên UI checkout.
        // Thuộc flow QR onboarding -> chọn gói.
        PlanTitleLabel.Text = _plan.Title;
        PlanSubtitleLabel.Text = _plan.PriceLabel;
        PriceLabel.Text = string.Format(_localizationService.GetString("PaymentCheckout_PriceAmountFormat"), _plan.Amount);
        PlanDescriptionLabel.Text = _plan.Description;
    }

    private async void OnPayClicked(object sender, EventArgs e)
    {
        // Xử lý nút thanh toán: tạo request, gọi API complete payment, validate session rồi điều hướng tiếp.
        // Đây là bước chính để khởi tạo session sau khi user trả phí.
        if (_isProcessingPayment)
        {
            return;
        }

        _isProcessingPayment = true;

        try
        {
            // Mock payment processing delay
            await Task.Delay(1500);

            var deviceId = await _sessionStore.GetOrCreateDeviceIdAsync();
            var payload = App.PendingQrPayload;

            // Call real API to complete payment (mocking the "Paid" status but registering with server)
            var request = new PaymentCompletionRequest(
                DeviceId: deviceId,
                SessionToken: Guid.NewGuid().ToString("N"), // Start fresh session
                RefreshToken: Guid.NewGuid().ToString("N"),
                QrToken: payload?.IdentityToken,
                UserAppId: null,
                LocationId: payload?.LocationId,
                AudioGuideId: payload?.AudioGuideId,
                AudioUrl: payload?.AudioUrl,
                PackageId: _plan.Id ?? "daily",
                PaymentStatus: "Paid", // Always succeed in mock mode
                PaymentReference: $"PAY_{DateTime.UtcNow:yyyyMMddHHmmss}_{deviceId.Substring(0, 4)}"
            );

            var result = await _apiService.CompletePaymentAsync(request);

            if (result != null && result.Success)
            {
                await _sessionStore.SaveSnapshotAsync(new AppSessionSnapshot(
                    deviceId,
                    result.SessionToken,
                    result.AccessToken,
                    result.RefreshToken,
                    payload?.IdentityToken,
                    result.UserAppId,
                    result.PackageId,
                    result.PaymentStatus,
                    result.PaymentReference,
                    payload?.LocationId,
                    payload?.AudioGuideId,
                    payload?.AudioUrl,
                    result.ExpiresAtUtc,
                    result.LastValidatedAtUtc));

                App.ClearPendingQrPayload();

                await DisplayAlert(
                    _localizationService.GetString("PaymentCheckout_AlertSuccessTitle"),
                    string.Format(_localizationService.GetString("PaymentCheckout_AlertSuccessMessageFormat"), _plan.Title),
                    _localizationService.GetString("Common_Close"));
                
                App.NavigateToLanguageSelection();
            }
            else
            {
                await DisplayAlert(
                    _localizationService.GetString("Common_Notice"),
                    result?.Message ?? "Payment registration failed. Please try again.",
                    _localizationService.GetString("Common_Close"));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                _localizationService.GetString("Common_Notice"),
                "Payment Error: " + ex.Message,
                _localizationService.GetString("Common_Close"));
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
