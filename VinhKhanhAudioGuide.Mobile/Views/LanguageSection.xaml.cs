using System.Globalization;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class LanguageSection : ContentPage
{
    public LanguageSection()
    {
        InitializeComponent();
    }

    private void OnVietnameseTapped(object sender, TappedEventArgs e)
    {
        ApplyLanguage("vi-VN");
    }

    private void OnEnglishTapped(object sender, TappedEventArgs e)
    {
        ApplyLanguage("en-US");
    }

    private void ApplyLanguage(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        Preferences.Default.Set("AppLanguage", cultureName);
        Preferences.Default.Set("IsFirstLaunch_v2", false);

        App.NavigateToShellRoot();
    }
}