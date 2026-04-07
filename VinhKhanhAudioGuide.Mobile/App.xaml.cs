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
#if DEBUG
        // Luôn hiện IntroPage ở chế độ Debug để test
        var isFirstLaunch = true;
#else
        var isFirstLaunch = Preferences.Default.Get(FirstLaunchPreferenceKey, true);
#endif
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

    public static void NavigateToLanguageSelection()
    {
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
                app.OpenWindow(new Window(new Views.LanguageSection()));
                return;
            }

            targetWindow.Page = new Views.LanguageSection();
        });
    }
}
