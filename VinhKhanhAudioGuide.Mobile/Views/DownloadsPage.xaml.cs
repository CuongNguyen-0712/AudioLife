using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class DownloadsPage : ContentPage
{
    public DownloadsPage(DownloadsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
