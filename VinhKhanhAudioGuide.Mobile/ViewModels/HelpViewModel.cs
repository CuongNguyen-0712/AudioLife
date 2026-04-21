using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.ViewModels;

public partial class HelpViewModel : LoadStateViewModel
{
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private List<FaqItem> _faqItems;

    public HelpViewModel(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        _faqItems = BuildFaqItems();

        CompleteLoading(_faqItems.Count > 0);
    }

    private List<FaqItem> BuildFaqItems()
    {
        return new List<FaqItem>
        {
            new() { Question = _localizationService.GetString("Help_Faq1_Question"), Answer = _localizationService.GetString("Help_Faq1_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq2_Question"), Answer = _localizationService.GetString("Help_Faq2_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq3_Question"), Answer = _localizationService.GetString("Help_Faq3_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq4_Question"), Answer = _localizationService.GetString("Help_Faq4_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq5_Question"), Answer = _localizationService.GetString("Help_Faq5_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq6_Question"), Answer = _localizationService.GetString("Help_Faq6_Answer") },
            new() { Question = _localizationService.GetString("Help_Faq7_Question"), Answer = _localizationService.GetString("Help_Faq7_Answer") }
        };
    }

    [RelayCommand]
    private void ToggleFaqItem(FaqItem? item)
    {
        if (item == null) return;
        item.IsExpanded = !item.IsExpanded;
    }

    [RelayCommand]
    private async Task SendEmailAsync()
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = _localizationService.GetString("Help_EmailSubject"),
                Body = "",
                To = new List<string> { "support@vinhkhanhaudioguide.com" }
            };
            await Email.Default.ComposeAsync(message);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("Common_Notice"),
                _localizationService.GetString("Help_AlertEmailFailed"),
                _localizationService.GetString("Common_Understood"));
        }
    }

    [RelayCommand]
    private async Task CallHotlineAsync()
    {
        try
        {
            PhoneDialer.Default.Open("19001234");
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("Common_Notice"),
                _localizationService.GetString("Help_AlertCallFailed"),
                _localizationService.GetString("Common_Understood"));
        }
    }

    [RelayCommand]
    private async Task OpenUserGuideAsync()
    {
        try
        {
            await Browser.Default.OpenAsync("https://vinhkhanhaudioguide.com/guide", BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            await Application.Current!.MainPage!.DisplayAlert(
                _localizationService.GetString("Common_Notice"),
                _localizationService.GetString("Help_AlertBrowserFailed"),
                _localizationService.GetString("Common_Understood"));
        }
    }
}

public class FaqItem : ObservableObject
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
}
