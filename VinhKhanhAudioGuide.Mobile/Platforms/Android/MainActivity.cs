using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using VinhKhanhAudioGuide.Mobile.Constants;

namespace VinhKhanhAudioGuide.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", 
          MainLauncher = true, 
          LaunchMode = LaunchMode.SingleTop,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = DeepLinkConstants.UrlScheme,
    DataHost = DeepLinkConstants.UrlHost,
    DataPathPrefix = DeepLinkConstants.AudioPath)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    private static void HandleIntent(Intent? intent)
    {
        var deepLink = intent?.DataString;
        if (string.IsNullOrWhiteSpace(deepLink))
        {
            return;
        }

        App.HandleIncomingDeepLink(deepLink);
    }
}
