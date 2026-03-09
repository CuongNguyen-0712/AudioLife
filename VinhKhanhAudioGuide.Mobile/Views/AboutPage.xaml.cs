using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
