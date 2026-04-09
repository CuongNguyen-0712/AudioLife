using Microsoft.Maui.Controls.Xaml;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Extensions;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension
{
    private static readonly Lazy<ILocalizationService> FallbackLocalization =
        new(() => new LocalizationService());

    public string Key { get; set; } = string.Empty;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            return string.Empty;
        }

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var localization = services?.GetService(typeof(ILocalizationService)) as ILocalizationService;
        return (localization ?? FallbackLocalization.Value).GetString(Key);
    }
}
