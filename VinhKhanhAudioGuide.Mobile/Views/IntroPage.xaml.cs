using Microsoft.Maui.ApplicationModel;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class IntroPage : ContentPage
{
    public IntroPage()
    {
        InitializeComponent();
    }

    private async void OnAllowClicked(object sender, EventArgs e)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        CompleteIntro();
    }

    private void OnLaterClicked(object sender, EventArgs e)
    {
        CompleteIntro();
    }

    private void CompleteIntro()
    {
        Preferences.Default.Set("IsFirstLaunch_v2", false);
        App.NavigateToShellRoot();
    }
}