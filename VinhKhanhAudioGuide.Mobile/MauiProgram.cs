using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using ZXing.Net.Maui.Controls;
#if ANDROID
using Android.Widget;
using Android.Graphics;
#endif

namespace VinhKhanhAudioGuide.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader();

        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            fonts.AddFont("RobotoCondensed-SemiBold.ttf", "RobotoCondensedSemiBold");
            fonts.AddFont("RobotoCondensed-Medium.ttf", "RobotoCondensedMedium");
            fonts.AddFont("RobotoCondensed-Regular.ttf", "RobotoCondensedRegular");
        });

        // Register services
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<Services.ILocalDatabaseService, Services.LocalDatabaseService>();
        builder.Services.AddSingleton<Services.IAudioService, Services.AudioService>();
        builder.Services.AddSingleton<Services.INavigationService, Services.NavigationService>();
        builder.Services.AddSingleton<Services.ILocalizationService, Services.LocalizationService>();
        builder.Services.AddSingleton<Services.IAppSessionStore, Services.AppSessionStore>();
        builder.Services.AddSingleton<Services.IAppHeartbeatService, Services.AppHeartbeatService>();
        builder.Services.AddSingleton<Services.ISearchService, Services.SearchService>();
        builder.Services.AddSingleton<Services.ApiService>();
        builder.Services.AddSingleton<Services.IApiService, Services.RemoteApiService>();
        builder.Services.AddSingleton<Services.IGeolocationService, Services.GeolocationService>();
        builder.Services.AddSingleton<Services.ITourCheckpointService, Services.TourCheckpointService>();
        builder.Services.AddSingleton<Services.ITourPlaybackSessionService, Services.TourPlaybackSessionService>();

        // Register ViewModels
        builder.Services.AddTransient<ViewModels.MainViewModel>();
        builder.Services.AddTransient<ViewModels.AudioPlayerViewModel>();
        builder.Services.AddTransient<ViewModels.LocationDetailViewModel>();
        builder.Services.AddTransient<ViewModels.MapViewModel>();
        builder.Services.AddTransient<ViewModels.ToursViewModel>();
        builder.Services.AddTransient<ViewModels.SettingsViewModel>();
        builder.Services.AddTransient<ViewModels.FavoritesViewModel>();
        builder.Services.AddTransient<ViewModels.SearchViewModel>();
        builder.Services.AddTransient<ViewModels.TourDetailViewModel>();
        builder.Services.AddTransient<ViewModels.DownloadsViewModel>();
        builder.Services.AddTransient<ViewModels.HistoryViewModel>();
        builder.Services.AddTransient<ViewModels.HelpViewModel>();
        builder.Services.AddTransient<ViewModels.AboutViewModel>();

        // Register Pages
        builder.Services.AddTransient<Views.MainPage>();
        builder.Services.AddTransient<Views.AudioPlayerPage>();
        builder.Services.AddTransient<Views.LocationDetailPage>();
        builder.Services.AddTransient<Views.MapPage>();
        builder.Services.AddTransient<Views.ToursPage>();
        builder.Services.AddTransient<Views.SettingsPage>();
        builder.Services.AddTransient<Views.FavoritesPage>();
        builder.Services.AddTransient<Views.SearchPage>();
        builder.Services.AddTransient<Views.TourDetailPage>();
        builder.Services.AddTransient<Views.DownloadsPage>();
        builder.Services.AddTransient<Views.HistoryPage>();
        builder.Services.AddTransient<Views.HelpPage>();
        builder.Services.AddTransient<Views.AboutPage>();
        builder.Services.AddTransient<Views.LanguageSection>();
        builder.Services.AddTransient<Views.PaymentPlanSelectionPage>();
        builder.Services.AddTransient<Views.PaymentCheckoutPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Remove Android underline for specific Entry (SearchInput)
#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            try
            {
                if (view?.AutomationId == "SearchInput")
                {
                    var platformView = handler.PlatformView;
                    if (platformView != null)
                    {
                        try
                        {
                            // Try multiple ways to clear native underline/background on Android
                            platformView.Background = null;
                        }
                        catch { }

                        try
                        {
                            // Set background color transparent
                            platformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                        }
                        catch { }

                        try
                        {
                            if (platformView is Android.Widget.EditText et)
                            {
                                et.SetBackgroundColor(Android.Graphics.Color.Transparent);
                                // API compatibility: remove background drawable
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        });
#endif

        var app = builder.Build();
        _ = app.Services.GetRequiredService<Services.ILocalizationService>();
        return app;
    }
    }
