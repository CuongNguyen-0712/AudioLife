using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class AudioPlayerPage : ContentPage
{
    public AudioPlayerPage(AudioPlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
