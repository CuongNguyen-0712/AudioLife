using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile;

public partial class App : Application
{
    private static bool _hasQrAccessInSession;
    private static string? _pendingDeepLink;
    private static bool _isProcessingDeepLink;

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

        _ = SafeProcessPendingDeepLinkAsync();
        return window;
    }

    public static void NavigateToQrScanner()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(CreateStyledNavigationPage(new Views.QrScannerPage())));
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
        _ = SafeProcessPendingDeepLinkAsync();
    }

    public static void NavigateToShellRoot()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(new AppShell()));
    }

    public static void NavigateToLanguageSelection()
    {
        MainThread.BeginInvokeOnMainThread(() => SetRootPage(new Views.LanguageSection()));
    }

    public static async Task CompleteQrOnboardingAsync(QrAudioPayload payload)
    {
        _hasQrAccessInSession = true;
        _ = payload;
        await OpenPaymentSelectionAfterQrAsync();
    }

    private static async Task ProcessPendingDeepLinkAsync()
    {
        await WaitForWindowReadyAsync();

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

    private static async Task SafeProcessPendingDeepLinkAsync()
    {
        if (_isProcessingDeepLink)
        {
            return;
        }

        _isProcessingDeepLink = true;
        try
        {
            await ProcessPendingDeepLinkAsync();
        }
        catch
        {
            // Never crash app startup due to deep-link timing or transient navigation errors.
        }
        finally
        {
            _isProcessingDeepLink = false;
        }
    }

    private static async Task WaitForWindowReadyAsync()
    {
        for (var i = 0; i < 15; i++)
        {
            var hasWindow = Current?.Windows.Any(w => w?.Page is not null) == true;
            if (hasWindow)
            {
                return;
            }

            await Task.Delay(100);
        }
    }

    private static async Task OpenPaymentSelectionAfterQrAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!_hasQrAccessInSession)
            {
                return;
            }

            var app = Current;
            var currentPage = app?.Windows.FirstOrDefault(w => w?.Page is not null)?.Page;
            if (currentPage is NavigationPage navigationPage)
            {
                await navigationPage.PushAsync(new Views.PaymentPlanSelectionPage());
                return;
            }

            SetRootPage(CreateStyledNavigationPage(new Views.PaymentPlanSelectionPage()));
        });
    }

    private static NavigationPage CreateStyledNavigationPage(Page rootPage)
    {
        var navPage = new NavigationPage(rootPage)
        {
            BarBackgroundColor = Application.Current?.Resources.TryGetValue("SurfaceContainerLowest", out var barBg) == true && barBg is Color bg
                ? bg
                : Colors.White,
            BarTextColor = Application.Current?.Resources.TryGetValue("OnSurface", out var barText) == true && barText is Color fg
                ? fg
                : Colors.Black
        };

        return navPage;
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
