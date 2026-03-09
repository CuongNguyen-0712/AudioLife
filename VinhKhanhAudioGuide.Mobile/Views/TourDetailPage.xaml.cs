using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class TourDetailPage : ContentPage
{
    public TourDetailPage(TourDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
