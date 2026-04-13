using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class AudioPlayerPage : ContentPage
{
    private readonly AudioPlayerViewModel _viewModel;

    public AudioPlayerPage(AudioPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        _viewModel.OnDisappearing();
        base.OnDisappearing();
    }
}
