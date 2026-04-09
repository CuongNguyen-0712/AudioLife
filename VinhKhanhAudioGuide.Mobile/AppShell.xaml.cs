using VinhKhanhAudioGuide.Mobile.Views;

namespace VinhKhanhAudioGuide.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute(nameof(AudioPlayerPage), typeof(AudioPlayerPage));
        Routing.RegisterRoute(nameof(LocationDetailPage), typeof(LocationDetailPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(FavoritesPage), typeof(FavoritesPage));
        //Routing.RegisterRoute(nameof(TourDetailPage), typeof(TourDetailPage));
        Routing.RegisterRoute(nameof(DownloadsPage), typeof(DownloadsPage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
    }
}
