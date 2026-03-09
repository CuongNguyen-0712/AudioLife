using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class LocationDetailPage : ContentPage
{
    public LocationDetailPage(LocationDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
