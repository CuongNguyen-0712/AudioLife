namespace VinhKhanhAudioGuide.Web.Services;

public interface ITextToSpeechService
{
    /// <summary>
    /// Synthesize text to speech and return MP3 audio bytes.
    /// </summary>
    /// <param name="text">Text content to convert to speech</param>
    /// <param name="language">Language code (e.g., "vi", "en")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MP3 audio bytes</returns>
    Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available voice name for a language code.
    /// </summary>
    string GetVoiceForLanguage(string language);

    /// <summary>
    /// Get supported languages with display names.
    /// </summary>
    Dictionary<string, string> GetSupportedLanguages();
}
