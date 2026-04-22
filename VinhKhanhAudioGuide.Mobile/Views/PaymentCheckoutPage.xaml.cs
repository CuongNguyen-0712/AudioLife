using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentCheckoutPage : ContentPage
{
    private readonly PaymentPlanOption _plan;
    private readonly IApiService _apiService;
    private readonly IAppSessionStore _sessionStore;
    private bool _isProcessingPayment;

    public PaymentCheckoutPage(PaymentPlanOption plan)
    {
        InitializeComponent();
        _plan = plan;
        _apiService = ResolveApiService();
        _sessionStore = ResolveSessionStore();
        ApplyPlan();
    }

    private void ApplyPlan()
    {
        PlanTitleLabel.Text = _plan.Title;
        PlanSubtitleLabel.Text = _plan.PriceLabel;
        PriceLabel.Text = $"{_plan.Amount:N0}đ";
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
                await DisplayAlert("Thiếu gói thanh toán", "Không xác định được gói thanh toán đã chọn.", "Đóng");
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
                await DisplayAlert("Thanh toán thất bại", result?.Message ?? "Không thể xác nhận thanh toán.", "Đóng");
                return;
            }

            var validation = await _apiService.ValidateSessionAsync(result.SessionToken, deviceId);
            if (validation is null || !validation.IsValid)
            {
                await DisplayAlert(
                    "Thanh toán chưa hoàn tất",
                    validation?.Message ?? "Thanh toán chưa được xác minh từ máy chủ. Dữ liệu chưa được ghi DB.",
                    "Đóng");
                return;
            }

            if (Application.Current?.Handler?.MauiContext?.Services.GetService<IAppSessionStore>() is IAppSessionStore sessionStore)
            {
                await sessionStore.SaveSnapshotAsync(new AppSessionSnapshot(
                    deviceId,
                    result.SessionToken,
                    result.RefreshToken,
                    payload?.IdentityToken,
                    result.UserAppId,
                    result.PackageId,
                    result.PaymentStatus,
                    request.PaymentReference,
                    payload?.LocationId,
                    payload?.AudioGuideId,
                    payload?.AudioUrl,
                    result.ExpiresAtUtc,
                    result.LastValidatedAtUtc));
            }

            App.ClearPendingQrPayload();

            await DisplayAlert("Thanh toán thành công", $"Gói {_plan.Title} đã được kích hoạt.", "Đóng");
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
}
