namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class StartupLoadingPage : ContentPage
{
    private bool _hasInitialized;

    public StartupLoadingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await App.InitializeStartupAsync();
    }
}
