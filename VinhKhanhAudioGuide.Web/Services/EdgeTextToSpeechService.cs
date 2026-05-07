using EdgeTTS;
using System.Net;
using System.Text;

namespace VinhKhanhAudioGuide.Web.Services;

public class EdgeTextToSpeechService : ITextToSpeechService
{
    private static readonly Dictionary<string, string> LanguageVoiceMap = new()
    {
        { "vi", "vi-VN-HoaiMyNeural" },
        { "en", "en-US-JennyNeural" },
        { "fr", "fr-FR-DeniseNeural" },
        { "ja", "ja-JP-NanamiNeural" },
        { "ko", "ko-KR-SunHiNeural" },
        { "zh", "zh-CN-XiaoxiaoNeural" }
    };

    private static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        { "vi", "Tiếng Việt" },
        { "en", "English" },
        { "fr", "Français" },
        { "ja", "日本語" },
        { "ko", "한국어" },
        { "zh", "中文" }
    };

    private static readonly Dictionary<string, string> GoogleLanguageMap = new()
    {
        { "vi", "vi" },
        { "en", "en" },
        { "fr", "fr" },
        { "ja", "ja" },
        { "ko", "ko" },
        { "zh", "zh-CN" }
    };

    private readonly ILogger<EdgeTextToSpeechService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EdgeTextToSpeechService(ILogger<EdgeTextToSpeechService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> SynthesizeAsync(string text, string language, CancellationToken cancellationToken = default)
    {
        // Sinh audio từ transcript bằng Edge TTS; nếu 403 thì chuyển sang Google fallback.
        // Thuộc flow tạo AudioGuide tự động khi duyệt change request.
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text không được để trống.", nameof(text));
        }

        var voice = GetVoiceForLanguage(language);

        _logger.LogInformation("TTS: Generating audio for language '{Language}' with voice '{Voice}', text length: {Length}",
            language, voice, text.Length);

        try
        {
            var communicate = new Communicate(text, voice);
            using var memoryStream = new MemoryStream();
            var tcs = new TaskCompletionSource();

            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            await communicate.Stream(
                chunk =>
                {
                    if (chunk.Data != null)
                    {
                        chunk.Data.CopyTo(memoryStream);
                    }
                },
                cancellationToken
            );

            if (memoryStream.Length == 0)
            {
                throw new InvalidOperationException("TTS không tạo được dữ liệu audio.");
            }

            _logger.LogInformation("TTS: Successfully generated {Bytes} bytes of audio", memoryStream.Length);
            return memoryStream.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex.Message.Contains("status code '403'", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "TTS: Edge endpoint rejected request with 403 for language '{Language}'", language);

            try
            {
                var fallbackAudio = await SynthesizeWithGoogleFallbackAsync(text, language, cancellationToken);
                if (fallbackAudio.Length > 0)
                {
                    _logger.LogInformation("TTS: Google fallback succeeded for language '{Language}'", language);
                    return fallbackAudio;
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "TTS: Google fallback failed for language '{Language}'", language);
            }

            throw new InvalidOperationException(
                "Dịch vụ TTS Edge đang từ chối kết nối (403) và fallback cũng thất bại. " +
                "Vui lòng kiểm tra mạng/VPN/firewall hoặc dùng chế độ upload file audio thủ công.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS: Failed to generate audio for language '{Language}'", language);
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public string GetVoiceForLanguage(string language)
    {
        if (LanguageVoiceMap.TryGetValue(language, out var voice))
        {
            return voice;
        }

        _logger.LogWarning("TTS: Unsupported language '{Language}', falling back to English", language);
        return LanguageVoiceMap["en"];
    }

    public Dictionary<string, string> GetSupportedLanguages()
    {
        return new Dictionary<string, string>(SupportedLanguages);
    }

    private async Task<byte[]> SynthesizeWithGoogleFallbackAsync(string text, string language, CancellationToken cancellationToken)
    {
        // Fallback Google TTS theo từng chunk, sau đó ghép MP3 để trả về 1 file hoàn chỉnh.
        // Giúp hệ thống vẫn tạo audio khi Edge bị chặn.
        var googleLanguage = GoogleLanguageMap.TryGetValue(language, out var mapped) ? mapped : "en";
        var chunks = SplitTextForGoogleTts(text, 180);
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        using var mergedStream = new MemoryStream();
        var isFirstChunk = true;

        foreach (var chunk in chunks)
        {
            var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={Uri.EscapeDataString(googleLanguage)}&q={Uri.EscapeDataString(chunk)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException($"Google fallback bị từ chối: {(int)response.StatusCode}.");
            }

            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            if (bytes.Length == 0)
            {
                continue;
            }

            if (!isFirstChunk)
            {
                bytes = StripId3Header(bytes);
            }

            await mergedStream.WriteAsync(bytes, cancellationToken);
            isFirstChunk = false;
        }

        if (mergedStream.Length == 0)
        {
            throw new InvalidOperationException("Google fallback không tạo được dữ liệu audio.");
        }

        return mergedStream.ToArray();
    }

    private static List<string> SplitTextForGoogleTts(string text, int maxChars)
    {
        var cleaned = text.Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleaned.Length <= maxChars)
        {
            return new List<string> { cleaned };
        }

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }

            if (current.Length + 1 + word.Length <= maxChars)
            {
                current.Append(' ').Append(word);
                continue;
            }

            chunks.Add(current.ToString());
            current.Clear();

            if (word.Length <= maxChars)
            {
                current.Append(word);
            }
            else
            {
                var index = 0;
                while (index < word.Length)
                {
                    var take = Math.Min(maxChars, word.Length - index);
                    chunks.Add(word.Substring(index, take));
                    index += take;
                }
            }
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks;
    }

    private static byte[] StripId3Header(byte[] bytes)
    {
        if (bytes.Length < 10)
        {
            return bytes;
        }

        var hasId3 = bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33;
        if (!hasId3)
        {
            return bytes;
        }

        var size = (bytes[6] & 0x7F) << 21 |
                   (bytes[7] & 0x7F) << 14 |
                   (bytes[8] & 0x7F) << 7 |
                   (bytes[9] & 0x7F);
        var start = Math.Min(bytes.Length, 10 + size);
        if (start <= 0 || start >= bytes.Length)
        {
            return bytes;
        }

        var result = new byte[bytes.Length - start];
        Buffer.BlockCopy(bytes, start, result, 0, result.Length);
        return result;
    }
}
