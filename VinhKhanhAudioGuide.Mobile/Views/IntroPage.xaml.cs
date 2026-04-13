using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Views;

public partial class IntroPage : ContentPage
{
    private readonly ILocalizationService _localizationService;
    private bool _isBusy;

    public IntroPage()
    {
        InitializeComponent();
        _localizationService = ResolveLocalizationService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLocalizedTexts();
        SetLoadingState(false);
    }

    private async void OnAllowClicked(object sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        SetLoadingState(true);
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status == PermissionStatus.Granted)
        {
            App.NavigateToQrScanner();
            return;
        }

        await DisplayAlert(
            _localizationService.GetString("Common_Notice"),
            _localizationService.GetString("Intro_AlertCameraRequired"),
            _localizationService.GetString("Common_Understood"));
        SetLoadingState(false);
    }

    private async void OnLaterClicked(object sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        await DisplayAlert(
            _localizationService.GetString("Common_Notice"),
            _localizationService.GetString("Intro_AlertCannotContinue"),
            _localizationService.GetString("Common_Understood"));
    }

    private void ApplyLocalizedTexts()
    {
        HeaderTitleLabel.Text = _localizationService.GetString("Intro_PageTitle");
        MainTitlePrefixSpan.Text = _localizationService.GetString("Intro_MainTitlePrefix");
        MainTitleHighlightSpan.Text = _localizationService.GetString("Intro_MainTitleHighlight");
        DescriptionLabel.Text = _localizationService.GetString("Intro_Description");
        AllowCameraButton.Text = _localizationService.GetString("Intro_AllowCamera");
        LaterButton.Text = _localizationService.GetString("Intro_Later");
        PrivacyBadgeLabel.Text = _localizationService.GetString("Intro_PrivacyBadge");
        LoadingTextLabel.Text = _localizationService.GetString("Common_Loading");
    }

    private void SetLoadingState(bool isBusy)
    {
        _isBusy = isBusy;
        LoadingOverlay.IsVisible = isBusy;
        LoadingIndicator.IsRunning = isBusy;
    }

    private static ILocalizationService ResolveLocalizationService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<ILocalizationService>()
            ?? new LocalizationService();
    }
}