using System.Globalization;
using System.Reflection;
using System.Resources;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string AppLanguageKey = "AppLanguage";
    private const string LegacyLanguageKey = "Language";
    private const string DefaultCultureName = "vi-VN";

    private static readonly SupportedLanguage[] SupportedLanguages =
    {
        new("vi-VN", "Tiếng Việt"),
        new("en-US", "English"),
        new("zh-CN", "简体中文")
    };

    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture = CultureInfo.GetCultureInfo(DefaultCultureName);

    public event EventHandler? CultureChanged;

    public static string GetPersistedOrDefaultCulture()
    {
        var persisted = Preferences.Default.Get(AppLanguageKey, DefaultCultureName);
        return NormalizeCulture(persisted);
    }

    public static void ApplyPersistedCulture()
    {
        var culture = CultureInfo.GetCultureInfo(GetPersistedOrDefaultCulture());
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static void ApplyDefaultVietnamese(bool persist = true)
    {
        var culture = CultureInfo.GetCultureInfo(DefaultCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (persist)
        {
            Preferences.Default.Set(AppLanguageKey, culture.Name);
            Preferences.Default.Set(LegacyLanguageKey, "Tiếng Việt");
        }
    }

    public LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "VinhKhanhAudioGuide.Mobile.Resources.Localization.AppStrings",
            typeof(LocalizationService).GetTypeInfo().Assembly);

        var savedCulture = GetPersistedOrDefaultCulture();
        SetCultureInternal(savedCulture, persist: false);
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguages() => SupportedLanguages;

    public string GetCurrentLanguageDisplayName()
    {
        var normalizedCulture = NormalizeCulture(_currentCulture.Name);
        return SupportedLanguages.FirstOrDefault(language =>
            string.Equals(language.CultureName, normalizedCulture, StringComparison.OrdinalIgnoreCase))?.DisplayName
            ?? SupportedLanguages[0].DisplayName;
    }

    public string GetString(string key, CultureInfo? culture = null)
    {
        var value = _resourceManager.GetString(key, culture ?? _currentCulture);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var fallback = _resourceManager.GetString(key, CultureInfo.GetCultureInfo(DefaultCultureName));
        return string.IsNullOrWhiteSpace(fallback) ? key : fallback;
    }

    public void SetCulture(string cultureName)
    {
        SetCultureInternal(cultureName, persist: true);
    }

    private void SetCultureInternal(string cultureName, bool persist)
    {
        var normalizedCulture = NormalizeCulture(cultureName);
        var culture = CultureInfo.GetCultureInfo(normalizedCulture);

        if (string.Equals(_currentCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase) && persist)
        {
            return;
        }

        _currentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (persist)
        {
            Preferences.Default.Set(AppLanguageKey, culture.Name);
            Preferences.Default.Set(LegacyLanguageKey, GetCurrentLanguageDisplayName());
        }

        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return DefaultCultureName;
        }

        return cultureName.Trim().ToLowerInvariant() switch
        {
            "vi" or "vi-vn" => "vi-VN",
            "en" or "en-us" => "en-US",
            "zh" or "zh-cn" or "zh-hans" => "zh-CN",
            _ => SupportedLanguages.Any(language =>
                    string.Equals(language.CultureName, cultureName, StringComparison.OrdinalIgnoreCase))
                ? cultureName
                : DefaultCultureName
        };
    }
}
