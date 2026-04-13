using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class PaymentPlanSelectionPage : ContentPage
{
    private readonly PaymentPlanOption _dailyPlan = new("daily", "10.000đ/ngày", "Một ngày sử dụng", "Phù hợp khi bạn muốn trải nghiệm nhanh trong một ngày, tối ưu cho khách ghé ngắn.", 10000m);
    private readonly PaymentPlanOption _fullTourPlan = new("full-tour", "29.000đ/full tour", "Một lần thanh toán", "Mở khóa toàn bộ tour, phù hợp khi bạn muốn nghe trọn vẹn nội dung đã quét.", 29000m);

    private PaymentPlanOption _selectedPlan;

    public PaymentPlanSelectionPage()
    {
        InitializeComponent();
        _selectedPlan = _dailyPlan;
        ApplySelectionState();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplySelectionState();
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
        await Navigation.PushAsync(new PaymentCheckoutPage(_selectedPlan));
    }

    private void ApplySelectionState()
    {
        SetSelectedState(DailyPlanCard, DailyPlanIndicator, DailyPlanIndicatorLabel, _selectedPlan.Id == _dailyPlan.Id);
        SetSelectedState(FullTourPlanCard, FullTourPlanIndicator, FullTourPlanIndicatorLabel, _selectedPlan.Id == _fullTourPlan.Id);
        SelectionSummaryLabel.Text = $"Đang chọn: {_selectedPlan.Title}";
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
}
