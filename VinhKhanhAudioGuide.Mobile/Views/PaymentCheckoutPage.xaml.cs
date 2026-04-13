using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentCheckoutPage : ContentPage
{
    private readonly PaymentPlanOption _plan;

    public PaymentCheckoutPage(PaymentPlanOption plan)
    {
        InitializeComponent();
        _plan = plan;
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
        await DisplayAlert("Thanh toán thành công", $"Gói {_plan.Title} đã được kích hoạt.", "Đóng");
        App.NavigateToLanguageSelection();
    }
}
