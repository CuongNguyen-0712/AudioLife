using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
