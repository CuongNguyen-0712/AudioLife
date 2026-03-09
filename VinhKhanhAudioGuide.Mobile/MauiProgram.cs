using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace VinhKhanhAudioGuide.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<Services.IAudioService, Services.AudioService>();
        builder.Services.AddSingleton<Services.INavigationService, Services.NavigationService>();
        builder.Services.AddSingleton<Services.IApiService, Services.ApiService>();
        builder.Services.AddSingleton<Services.IGeolocationService, Services.GeolocationService>();

        // Register ViewModels
        builder.Services.AddTransient<ViewModels.MainViewModel>();
        builder.Services.AddTransient<ViewModels.AudioPlayerViewModel>();
        builder.Services.AddTransient<ViewModels.LocationDetailViewModel>();
        builder.Services.AddTransient<ViewModels.MapViewModel>();
        builder.Services.AddTransient<ViewModels.ToursViewModel>();
        builder.Services.AddTransient<ViewModels.ProfileViewModel>();
        builder.Services.AddTransient<ViewModels.SettingsViewModel>();
        builder.Services.AddTransient<ViewModels.FavoritesViewModel>();
        builder.Services.AddTransient<ViewModels.SearchViewModel>();
        builder.Services.AddTransient<ViewModels.TourDetailViewModel>();
        builder.Services.AddTransient<ViewModels.DownloadsViewModel>();
        builder.Services.AddTransient<ViewModels.HistoryViewModel>();
        builder.Services.AddTransient<ViewModels.HelpViewModel>();
        builder.Services.AddTransient<ViewModels.AboutViewModel>();
        builder.Services.AddTransient<ViewModels.EditProfileViewModel>();

        // Register Pages
        builder.Services.AddTransient<Views.MainPage>();
        builder.Services.AddTransient<Views.AudioPlayerPage>();
        builder.Services.AddTransient<Views.LocationDetailPage>();
        builder.Services.AddTransient<Views.MapPage>();
        builder.Services.AddTransient<Views.ToursPage>();
        builder.Services.AddTransient<Views.ProfilePage>();
        builder.Services.AddTransient<Views.SettingsPage>();
        builder.Services.AddTransient<Views.FavoritesPage>();
        builder.Services.AddTransient<Views.SearchPage>();
        builder.Services.AddTransient<Views.TourDetailPage>();
        builder.Services.AddTransient<Views.DownloadsPage>();
        builder.Services.AddTransient<Views.HistoryPage>();
        builder.Services.AddTransient<Views.HelpPage>();
        builder.Services.AddTransient<Views.AboutPage>();
        builder.Services.AddTransient<Views.EditProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
