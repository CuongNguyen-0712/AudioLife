using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class MapPage : ContentPage
{
    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
