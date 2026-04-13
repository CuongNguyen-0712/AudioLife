using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class LanguageSection : ContentPage
{
    private readonly ILocalizationService _localizationService;
    private bool _isApplyingLanguage;

    public LanguageSection() : this(ResolveLocalizationService())
    {
    }

    public LanguageSection(ILocalizationService localizationService)
    {
        InitializeComponent();
        _localizationService = localizationService;
        ApplyLocalizedTexts();
    }

    private void OnVietnameseTapped(object sender, TappedEventArgs e)
    {
        _ = ApplyLanguageAsync("vi-VN");
    }

    private void OnEnglishTapped(object sender, TappedEventArgs e)
    {
        _ = ApplyLanguageAsync("en-US");
    }

    private void OnChineseSimplifiedTapped(object sender, TappedEventArgs e)
    {
        _ = ApplyLanguageAsync("zh-CN");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLocalizedTexts();
        SetLoadingState(false);
    }

    private async Task ApplyLanguageAsync(string cultureName)
    {
        if (_isApplyingLanguage)
        {
            return;
        }

        SetLoadingState(true);
        _localizationService.SetCulture(cultureName);
        ApplyLocalizedTexts();
        Preferences.Default.Set("IsFirstLaunch_v2", false);

        await Task.Yield();
        App.NavigateToShellRoot();
    }

    private void ApplyLocalizedTexts()
    {
        TitleLabel.Text = _localizationService.GetString("Language_Page_Title");
        VietnameseOptionLabel.Text = _localizationService.GetString("Language_Option_Vietnamese");
        EnglishOptionLabel.Text = _localizationService.GetString("Language_Option_English");
        ChineseSimplifiedOptionLabel.Text = _localizationService.GetString("Language_Option_ChineseSimplified");
        LoadingTextLabel.Text = _localizationService.GetString("Language_Applying");
    }

    private void SetLoadingState(bool isLoading)
    {
        _isApplyingLanguage = isLoading;
        LoadingOverlay.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
    }

    private static ILocalizationService ResolveLocalizationService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
            ?? new LocalizationService();
    }
}