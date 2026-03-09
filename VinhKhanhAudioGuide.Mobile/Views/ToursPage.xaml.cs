using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class ToursPage : ContentPage
{
    public ToursPage(ToursViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
