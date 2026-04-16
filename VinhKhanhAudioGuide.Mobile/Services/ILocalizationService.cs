using System.Globalization;

namespace VinhKhanhAudioGuide.Mobile.Services;

public interface ILocalizationService
{
    event EventHandler? CultureChanged;

    CultureInfo CurrentCulture { get; }

    IReadOnlyList<SupportedLanguage> GetSupportedLanguages();

    string GetCurrentLanguageDisplayName();

    string GetString(string key, CultureInfo? culture = null);

    void SetCulture(string cultureName);

    void ResetToDefaultCulture();
}

public sealed record SupportedLanguage(string CultureName, string DisplayName);
