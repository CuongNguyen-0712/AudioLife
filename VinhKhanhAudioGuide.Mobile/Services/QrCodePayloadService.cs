using VinhKhanhAudioGuide.Mobile.Constants;
using QRCoder;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed record QrAudioPayload(string LocationId, string AudioGuideId, string AudioUrl);

public static class QrCodePayloadService
{
    public static string BuildAudioDeepLink(string locationId, string? audioGuideId = null, string? audioUrl = null)
    {
        var queryParts = new List<string>
        {
            $"{DeepLinkConstants.LocationIdParam}={Uri.EscapeDataString(locationId)}"
        };

        if (!string.IsNullOrWhiteSpace(audioGuideId))
        {
            queryParts.Add($"{DeepLinkConstants.AudioGuideIdParam}={Uri.EscapeDataString(audioGuideId)}");
        }

        if (!string.IsNullOrWhiteSpace(audioUrl))
        {
            queryParts.Add($"{DeepLinkConstants.AudioUrlParam}={Uri.EscapeDataString(audioUrl)}");
        }

        return $"{DeepLinkConstants.UrlScheme}://{DeepLinkConstants.UrlHost}{DeepLinkConstants.AudioPath}?{string.Join("&", queryParts)}";
    }

    public static byte[] GenerateQrCodePng(string deepLink, int pixelsPerModule = 16)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(pixelsPerModule);
    }

    public static bool TryParseAudioPayload(string rawValue, out QrAudioPayload payload)
    {
        payload = new QrAudioPayload(string.Empty, string.Empty, string.Empty);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        if (!Uri.TryCreate(rawValue.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, DeepLinkConstants.UrlScheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(uri.Host, DeepLinkConstants.UrlHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(uri.AbsolutePath, DeepLinkConstants.AudioPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var queryMap = ParseQuery(uri.Query);
        if (!queryMap.TryGetValue(DeepLinkConstants.LocationIdParam, out var locationId)
            || string.IsNullOrWhiteSpace(locationId))
        {
            return false;
        }

        queryMap.TryGetValue(DeepLinkConstants.AudioGuideIdParam, out var audioGuideId);
        queryMap.TryGetValue(DeepLinkConstants.AudioUrlParam, out var audioUrl);

        payload = new QrAudioPayload(locationId, audioGuideId ?? string.Empty, audioUrl ?? string.Empty);
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var content = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(content))
        {
            return values;
        }

        foreach (var pair in content.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }
}