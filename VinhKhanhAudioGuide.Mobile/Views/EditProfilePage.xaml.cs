using VinhKhanhAudioGuide.Mobile.ViewModels;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class EditProfilePage : ContentPage
{
    public EditProfilePage(EditProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
