using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private CancellationTokenSource? _pulseCancellationTokenSource;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartPulseAnimation();
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        StopPulseAnimation();
        base.OnDisappearing();
    }

    private void StartPulseAnimation()
    {
        StopPulseAnimation();
        _pulseCancellationTokenSource = new CancellationTokenSource();
        _ = RunPulseAnimationAsync(_pulseCancellationTokenSource.Token);
    }

    private void StopPulseAnimation()
    {
        _pulseCancellationTokenSource?.Cancel();
        _pulseCancellationTokenSource?.Dispose();
        _pulseCancellationTokenSource = null;
    }

    private async Task RunPulseAnimationAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await AutoPulseDot.ScaleTo(1.2, 700, Easing.SinInOut);
                await AutoPulseDot.FadeTo(0.45, 700, Easing.SinInOut);
                await AutoPulseDot.ScaleTo(1.0, 700, Easing.SinInOut);
                await AutoPulseDot.FadeTo(1.0, 700, Easing.SinInOut);
            }
            catch
            {
                return;
            }
        }
    }
}
