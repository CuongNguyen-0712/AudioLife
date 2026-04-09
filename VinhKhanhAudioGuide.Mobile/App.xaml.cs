using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile;

public partial class App : Application
{
    private static bool _hasQrAccessInSession;
    private static string? _pendingDeepLink;

    public App()
    {
        LocalizationService.ApplyDefaultVietnamese();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var hasQrAccess = _hasQrAccessInSession;
        Page rootPage = hasQrAccess ? new AppShell() : new Views.IntroPage();
        var window = new Window(rootPage);

        _ = ProcessPendingDeepLinkAsync();
        return window;
    }

    public static void NavigateToQrScanner()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(new NavigationPage(new Views.QrScannerPage())));
    }

    public static void NavigateToIntro()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(new Views.IntroPage()));
    }

    public static void HandleIncomingDeepLink(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink))
        {
            return;
        }

        _pendingDeepLink = deepLink;
        _ = ProcessPendingDeepLinkAsync();
    }

    public static void NavigateToShellRoot()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(new AppShell()));
    }

    public static async Task CompleteQrOnboardingAsync(QrAudioPayload payload)
    {
        _hasQrAccessInSession = true;
        _ = payload;
        await OpenLanguageSelectionAfterQrAsync();
    }

    private static async Task ProcessPendingDeepLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(_pendingDeepLink))
        {
            return;
        }

        var candidate = _pendingDeepLink;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        if (!QrCodePayloadService.TryParseAudioPayload(candidate, out var payload))
        {
            _pendingDeepLink = null;
            return;
        }

        _pendingDeepLink = null;
        await CompleteQrOnboardingAsync(payload);
    }

    private static async Task OpenLanguageSelectionAfterQrAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!_hasQrAccessInSession)
            {
                return;
            }

            SetRootPage(new Views.LanguageSection());
        });
    }

    private static void SetRootPage(Page rootPage)
    {
        var app = Current;
        if (app is null)
        {
            return;
        }

        var targetWindow = app.Windows.FirstOrDefault(w => w?.Page is not null);
        if (targetWindow is null)
        {
            app.OpenWindow(new Window(rootPage));
            return;
        }

        targetWindow.Page = rootPage;
    }
}
