namespace VinhKhanhAudioGuide.Mobile;

public partial class App : Application
{
    private const string FirstLaunchPreferenceKey = "IsFirstLaunch_v2";

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var isFirstLaunch = Preferences.Default.Get(FirstLaunchPreferenceKey, true);
        Page rootPage = isFirstLaunch ? new Views.IntroPage() : new AppShell();
        return new Window(rootPage);
    }

    public static void NavigateToShellRoot()
    {
        var shell = new AppShell();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var app = Current;
            if (app is null)
            {
                return;
            }

            var targetWindow = app.Windows.FirstOrDefault(w => w?.Page is not null);
            if (targetWindow is null)
            {
                app.OpenWindow(new Window(shell));
                return;
            }

            targetWindow.Page = shell;
        });
    }
}
