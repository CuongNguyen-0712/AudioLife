using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class HelpPage : ContentPage
{
    public HelpPage(HelpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
