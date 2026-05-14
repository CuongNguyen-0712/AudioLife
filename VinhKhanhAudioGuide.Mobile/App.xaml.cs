using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Models;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile;

public partial class App : Application
{
    private static string? _pendingDeepLink;
    private static bool _isProcessingDeepLink;
    private static QrAudioPayload? _pendingQrPayload;

    public App()
    {
        LocalizationService.ApplyDefaultVietnamese(persist: false);
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new Views.StartupLoadingPage());

        _ = SafeProcessPendingDeepLinkAsync();
        return window;
    }

    public static bool HasPendingDeepLink => !string.IsNullOrWhiteSpace(_pendingDeepLink);

    public static QrAudioPayload? PendingQrPayload => _pendingQrPayload;

    public static void NavigateToQrScanner()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = StopHeartbeatAsync();
            ApplyPreAuthVietnameseCulture();
            SetRootPage(CreateStyledNavigationPage(new Views.QrScannerPage()));
        });
    }

    public static void NavigateToIntro()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = StopHeartbeatAsync();
            ApplyPreAuthVietnameseCulture();
            SetRootPage(new Views.IntroPage());
        });
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Ensure we have a persisted culture, default to Vietnamese if not.
            // This handles the case where user exits the app at LanguageSelection screen.
            if (string.IsNullOrWhiteSpace(LocalizationService.GetPersistedOrDefaultCulture()) || 
                Preferences.Default.Get<string?>("AppLanguage", null) == null)
            {
                LocalizationService.ApplyDefaultVietnamese(persist: true);
            }

            ApplyPersistedCultureForAuthenticatedShell();
            SetRootPage(new AppShell());
            _ = StartHeartbeatAsync();
        });
    }

    public static void StorePendingQrPayload(QrAudioPayload payload)
    {
        _pendingQrPayload = payload;
    }

    public static void ClearPendingQrPayload()
    {
        _pendingQrPayload = null;
    }

    public static void NavigateToLanguageSelection()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = StopHeartbeatAsync();
            ApplyPreAuthVietnameseCulture();
            SetRootPage(new Views.LanguageSection());
        });
    }

    public static async Task CompleteQrOnboardingAsync(QrAudioPayload payload)
    {
        StorePendingQrPayload(payload);

        await OpenPaymentSelectionAfterQrAsync();
    }

    public static async Task InitializeStartupAsync()
    {
        if (HasPendingDeepLink)
        {
            return;
        }

        // 1. Resolve services with a small retry to handle early startup race conditions
        IServiceProvider? services = null;
        for (int i = 0; i < 5; i++)
        {
            services = Current?.Handler?.MauiContext?.Services;
            if (services != null) break;
            await Task.Delay(100);
        }

        var sessionStore = services?.GetService<IAppSessionStore>();
        var apiService = services?.GetService<IApiService>();

        if (sessionStore is null || apiService is null)
        {
            // Critical failure: services not ready
            NavigateToIntro();
            return;
        }

        var deviceId = await sessionStore.GetOrCreateDeviceIdAsync();
        var localSnapshot = await sessionStore.GetSnapshotAsync();
        var now = DateTime.UtcNow;

        // 2. Initial decision based on local session
        bool hasSnapshot = localSnapshot is not null;
        bool isSessionValid = hasSnapshot && localSnapshot!.ExpiresAtUtc > now;
        bool isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        // If we have a valid session (or we are offline and have any session), enter the app.
        // If we are offline and have NO session, we still allow entering as a guest to use "db local fallback".
        if (isSessionValid || (!isOnline && hasSnapshot) || (!isOnline && !hasSnapshot))
        {
            NavigateToShellRoot();
            _ = StartAutoPlaybackAsync();
            
            // If we are offline and entered, we are done with startup.
            if (!isOnline) return;
        }

        // 3. Perform online verification/update if internet is available
        if (isOnline)
        {
            try
            {
                var check = await apiService.CheckDeviceSessionAsync(deviceId);
                
                if (check is null)
                {
                    // API check failed (timeout or error) - if we already entered, just stay there.
                    if (!isSessionValid && !hasSnapshot)
                    {
                        NavigateToIntro();
                    }
                    return;
                }

                if (!check.HasSession)
                {
                    // Server says no session exists for this device
                    await sessionStore.ClearSnapshotAsync();
                    _ = StopHeartbeatAsync();
                    
                    // If we were already in, force logout
                    await MainThread.InvokeOnMainThreadAsync(NavigateToIntro);
                    return;
                }

                // Session exists on server, validate it
                var validation = await apiService.ValidateSessionAsync(check.SessionToken, deviceId);
                if (validation is null || !validation.IsValid)
                {
                    await sessionStore.ClearSnapshotAsync();
                    _ = StopHeartbeatAsync();
                    
                    await MainThread.InvokeOnMainThreadAsync(NavigateToIntro);
                    return;
                }

                // Session is valid and confirmed by server, update local storage
                var newSnapshot = new AppSessionSnapshot(
                    deviceId,
                    validation.SessionToken,
                    validation.AccessToken ?? check.AccessToken,
                    validation.RefreshToken ?? check.RefreshToken,
                    null, // QrToken
                    validation.UserAppId ?? check.UserAppId,
                    validation.PackageId ?? check.PackageId,
                    validation.PaymentStatus ?? check.PaymentStatus,
                    null, // PaymentReference
                    null, // LocationId
                    null, // AudioGuideId
                    null, // AudioUrl
                    validation.ExpiresAtUtc,
                    validation.LastValidatedAtUtc
                );
                await sessionStore.SaveSnapshotAsync(newSnapshot);

                // If we haven't entered the app yet (due to invalid local session), do it now
                if (!isSessionValid && !hasSnapshot)
                {
                    NavigateToShellRoot();
                    _ = StartAutoPlaybackAsync();
                }
            }
            catch
            {
                // Network error - if we haven't entered yet and have no session, go to intro
                if (!isSessionValid && !hasSnapshot)
                {
                    NavigateToIntro();
                }
            }
        }
    }

    private static async Task StartAutoPlaybackAsync()
    {
        var services = Current?.Handler?.MauiContext?.Services;
        var autoPlayback = services?.GetService<IAutoPlaybackService>();
        var geoService = services?.GetService<IGeolocationService>();

        if (autoPlayback != null && geoService != null)
        {
            await geoService.StartTrackingAsync();
            autoPlayback.Start();
        }
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
            if (_pendingQrPayload is null)
            {
                return;
            }

            var app = Current;
            var currentPage = app?.Windows.FirstOrDefault(w => w?.Page is not null)?.Page;
            if (currentPage is NavigationPage navigationPage)
            {
                ApplyPreAuthVietnameseCulture();
                await navigationPage.PushAsync(new Views.PaymentPlanSelectionPage());
                return;
            }

            ApplyPreAuthVietnameseCulture();
            SetRootPage(CreateStyledNavigationPage(new Views.PaymentPlanSelectionPage()));
        });
    }

    private static void ApplyPreAuthVietnameseCulture()
    {
        var services = Current?.Handler?.MauiContext?.Services;
        if (services?.GetService<ILocalizationService>() is ILocalizationService localizationService)
        {
            localizationService.ResetToDefaultCulture();
            return;
        }

        LocalizationService.ApplyDefaultVietnamese(persist: false);
    }

    private static void ApplyPersistedCultureForAuthenticatedShell()
    {
        var persistedCulture = LocalizationService.GetPersistedOrDefaultCulture();
        var services = Current?.Handler?.MauiContext?.Services;
        if (services?.GetService<ILocalizationService>() is ILocalizationService localizationService)
        {
            localizationService.SetCulture(persistedCulture);
            return;
        }

        LocalizationService.ApplyPersistedCulture();
    }

    private static async Task StartHeartbeatAsync()
    {
        var services = Current?.Handler?.MauiContext?.Services;
        var heartbeatService = services?.GetService<IAppHeartbeatService>();
        if (heartbeatService is null)
        {
            return;
        }

        await heartbeatService.StartAsync(async () =>
        {
            await StopHeartbeatAsync();
            LocalizationService.ClearPersistedCulture();
            NavigateToIntro();
        });
    }

    private static async Task StopHeartbeatAsync()
    {
        var services = Current?.Handler?.MauiContext?.Services;
        var heartbeatService = services?.GetService<IAppHeartbeatService>();
        if (heartbeatService is null)
        {
            return;
        }

        await heartbeatService.StopAsync();
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
