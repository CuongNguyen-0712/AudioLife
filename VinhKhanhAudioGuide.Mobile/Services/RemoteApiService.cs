using System.Text.Json;
using System.Text;
using VinhKhanhAudioGuide.Mobile.Models;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// Remote API service that reads seed data from Web API backed by SQL Server.
/// Falls back to local ApiService when backend is unavailable.
/// </summary>
public class RemoteApiService : IApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private const string CategoriesCacheKey = "catalog.categories";
    private const string LocationsCacheKey = "catalog.locations";
    private const string ToursCacheKey = "catalog.tours";
    private const string PreferredApiBaseUrlKey = "RemoteApiBaseUrl";
    private const string DefaultPublicApiBaseUrl = "https://aorta-sank-surviving.ngrok-free.dev";

    private readonly ApiService _fallback;
    private readonly ILocalizationService _localizationService;
    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly HttpClient _httpClient;
    private string? _activeBaseUrl;

    public RemoteApiService(ApiService fallback, ILocalizationService localizationService, ILocalDatabaseService localDatabaseService)
    {
        _fallback = fallback;
        _localizationService = localizationService;
        _localDatabaseService = localDatabaseService;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
    }

    public static void SetPreferredApiBaseUrl(string? baseUrl)
    {
        var normalized = NormalizeBaseUrl(baseUrl);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            Preferences.Remove(PreferredApiBaseUrlKey);
            return;
        }

        Preferences.Set(PreferredApiBaseUrlKey, normalized);
    }

    public async Task<List<Location>> GetLocationsAsync()
    {
        // Lấy catalog location từ API, nếu lỗi thì fallback cache SQLite rồi mới tới SampleData local.
        // Thuộc flow tải dữ liệu trang Home/Map theo chiến lược remote -> cache -> local.
        var remote = await TryGetAsync<List<Location>>(WithLanguage("api/mobile/locations"));
        if (remote is not null)
        {
            var normalized = NormalizeLocations(remote);
            await UpsertCacheAsync(LocationsCacheKey, normalized);
            return ContentLocalizationMapper.LocalizeLocations(normalized, GetCurrentLanguageCode());
        }

        var cached = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        if (cached is { Count: > 0 })
        {
            var normalized = NormalizeLocations(cached);
            return ContentLocalizationMapper.LocalizeLocations(normalized, GetCurrentLanguageCode());
        }

        return await _fallback.GetLocationsAsync();
    }

    public async Task<Location?> GetLocationByIdAsync(string locationId)
    {
        var remote = await TryGetAsync<Location>(WithLanguage($"api/mobile/locations/{Uri.EscapeDataString(locationId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocation(NormalizeLocation(remote), GetCurrentLanguageCode());
        }

        var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        var cached = cachedLocations?
            .FirstOrDefault(location => string.Equals(location.Id, locationId, StringComparison.OrdinalIgnoreCase));
        if (cached is not null)
        {
            return ContentLocalizationMapper.LocalizeLocation(NormalizeLocation(cached), GetCurrentLanguageCode());
        }

        return await _fallback.GetLocationByIdAsync(locationId);
    }

    public async Task<List<Location>> SearchLocationsAsync(string query)
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/search?query={Uri.EscapeDataString(query)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(remote), GetCurrentLanguageCode());
        }

        var normalizedQuery = (query ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
            if (cachedLocations is { Count: > 0 })
            {
                var filtered = cachedLocations
                    .Where(location =>
                        location.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                        location.Description.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                        location.Address.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count > 0)
                {
                    return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(filtered), GetCurrentLanguageCode());
                }
            }
        }

        return await _fallback.SearchLocationsAsync(query ?? string.Empty);
    }

    public async Task<List<Location>> GetLocationsByCategoryAsync(string categoryId)
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/by-category/{Uri.EscapeDataString(categoryId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(remote), GetCurrentLanguageCode());
        }

        var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        if (cachedLocations is { Count: > 0 })
        {
            var filtered = cachedLocations
                .Where(location => string.Equals(location.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count > 0)
            {
                return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(filtered), GetCurrentLanguageCode());
            }
        }

        return await _fallback.GetLocationsByCategoryAsync(categoryId);
    }

    public async Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1)
    {
        // Trả danh sách POI gần user để phục vụ auto-play và màn hình map.
        // Khi API lỗi sẽ tự tính từ cache theo khoảng cách + ưu tiên POI.
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(remote), GetCurrentLanguageCode());
        }

        var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        if (cachedLocations is { Count: > 0 })
        {
            var nearby = cachedLocations
                .Select(location => new
                {
                    Location = location,
                    DistanceKm = CalculateDistance(latitude, longitude, location.Latitude, location.Longitude),
                    PoiRadiusKm = Math.Max(location.DetectionRadiusMeters, 0) / 1000d
                })
                .Where(item => item.DistanceKm <= radiusKm + item.PoiRadiusKm)
                .OrderBy(item => item.DistanceKm)
                .ThenByDescending(item => item.Location.Priority)
                .ThenBy(item => item.Location.Id, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Location)
                .ToList();

            if (nearby.Count > 0)
            {
                return ContentLocalizationMapper.LocalizeLocations(NormalizeLocations(nearby), GetCurrentLanguageCode());
            }
        }

        return await _fallback.GetNearbyLocationsAsync(latitude, longitude, radiusKm);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var remote = await TryGetAsync<List<Category>>(WithLanguage("api/mobile/categories"));
        if (remote is not null)
        {
            await UpsertCacheAsync(CategoriesCacheKey, remote);
            return ContentLocalizationMapper.LocalizeCategories(remote, GetCurrentLanguageCode());
        }

        var cached = await GetCachedAsync<List<Category>>(CategoriesCacheKey);
        if (cached is { Count: > 0 })
        {
            return ContentLocalizationMapper.LocalizeCategories(cached, GetCurrentLanguageCode());
        }

        return await _fallback.GetCategoriesAsync();
    }

    public async Task<List<Tour>> GetToursAsync()
    {
        var remote = await TryGetAsync<List<Tour>>(WithLanguage("api/mobile/tours"));
        if (remote is not null)
        {
            await UpsertCacheAsync(ToursCacheKey, remote);
            return ContentLocalizationMapper.LocalizeTours(remote, GetCurrentLanguageCode());
        }

        var cached = await GetCachedAsync<List<Tour>>(ToursCacheKey);
        if (cached is { Count: > 0 })
        {
            return ContentLocalizationMapper.LocalizeTours(cached, GetCurrentLanguageCode());
        }

        return await _fallback.GetToursAsync();
    }

    public async Task<Tour?> GetTourByIdAsync(string tourId)
    {
        var remote = await TryGetAsync<Tour>(WithLanguage($"api/mobile/tours/{Uri.EscapeDataString(tourId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeTours(new[] { remote }, GetCurrentLanguageCode()).FirstOrDefault();
        }

        var cached = await GetCachedAsync<List<Tour>>(ToursCacheKey);
        var cachedTour = cached?
            .FirstOrDefault(tour => string.Equals(tour.Id, tourId, StringComparison.OrdinalIgnoreCase));
        if (cachedTour is not null)
        {
            return ContentLocalizationMapper.LocalizeTours(new[] { cachedTour }, GetCurrentLanguageCode()).FirstOrDefault();
        }

        return await _fallback.GetTourByIdAsync(tourId);
    }

    public async Task<List<Tour>> GetFeaturedToursAsync()
    {
        var remote = await TryGetAsync<List<Tour>>(WithLanguage("api/mobile/tours/featured"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeTours(remote, GetCurrentLanguageCode());
        }

        var cached = await GetCachedAsync<List<Tour>>(ToursCacheKey);
        if (cached is { Count: > 0 })
        {
            var featured = cached.Where(tour => tour.IsFeatured).ToList();
            if (featured.Count > 0)
            {
                return ContentLocalizationMapper.LocalizeTours(featured, GetCurrentLanguageCode());
            }
        }

        return await _fallback.GetFeaturedToursAsync();
    }

    public async Task<List<PaymentPackage>> GetPaymentPackagesAsync()
    {
        var remote = await TryGetAsync<List<PaymentPackage>>("api/mobile/payment/packages");
        if (remote is not null)
        {
            return remote
                .Where(item => item.IsActive)
                .OrderBy(item => item.Price)
                .ToList();
        }

        return await _fallback.GetPaymentPackagesAsync();
    }

    public async Task<DeviceSessionCheckResult?> CheckDeviceSessionAsync(string deviceId)
    {
        // Kiểm tra thiết bị đã có session hợp lệ chưa trước khi vào app/checkout.
        // Thuộc flow startup + QR onboarding.
        var path = $"api/mobile/session/by-device?deviceId={Uri.EscapeDataString(deviceId)}";
        var remote = await TryGetAsync<DeviceSessionCheckResult>(path);
        if (remote is not null)
        {
            return remote;
        }

        return await _fallback.CheckDeviceSessionAsync(deviceId);
    }

    public async Task<PaymentCompletionResult?> CompletePaymentAsync(PaymentCompletionRequest request)
    {
        // Gửi kết quả thanh toán để server tạo/cập nhật subscription và session token.
        // Thuộc flow QR Scan -> Payment -> Session Start.
        var payload = new
        {
            request.DeviceId,
            request.SessionToken,
            request.RefreshToken,
            request.QrToken,
            request.UserAppId,
            request.LocationId,
            request.AudioGuideId,
            request.AudioUrl,
            request.PackageId,
            request.PaymentStatus,
            request.PaymentReference
        };

        var remote = await TryPostAsync<object, PaymentCompletionResult>("api/mobile/payment/complete", payload);
        if (remote is not null)
        {
            return remote;
        }

        return await _fallback.CompletePaymentAsync(request);
    }

    public async Task<SessionValidationResult?> ValidateSessionAsync(string sessionToken, string deviceId)
    {
        // Validate session token với server trước khi cho phép vào luồng chính.
        // Dùng ở startup và sau checkout để đảm bảo session còn hợp lệ.
        var path = $"api/mobile/session/validate?sessionToken={Uri.EscapeDataString(sessionToken)}&deviceId={Uri.EscapeDataString(deviceId)}";
        var remote = await TryGetAsync<SessionValidationResult>(path);
        if (remote is not null)
        {
            return remote;
        }

        return await _fallback.ValidateSessionAsync(sessionToken, deviceId);
    }

    public async Task<HeartbeatResponse?> SendHeartbeatAsync(HeartbeatRequest request)
    {
        // Ping định kỳ lên server để keep-alive session và log activity user.
        // Thuộc flow background heartbeat 5 giây/lần.
        var payload = new
        {
            request.DeviceId,
            request.SessionToken,
            request.ActivityName,
            request.ActivityContext,
            request.ScreenName,
            request.Route,
            request.IsForeground
        };

        var remote = await TryPostAsync<object, HeartbeatResponse>("api/mobile/heartbeat", payload);
        if (remote is not null)
        {
            return remote;
        }

        return await _fallback.SendHeartbeatAsync(request);
    }

    public async Task<bool> TestServerConnectionAsync()
    {
        var remote = await TryGetAsync<List<Category>>("api/mobile/categories");
        return remote is not null;
    }

    public async Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId)
    {
        var remote = await TryGetAsync<List<AudioGuide>>(WithLanguage($"api/mobile/audio/by-location/{Uri.EscapeDataString(locationId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeAudioGuides(remote, GetCurrentLanguageCode(), locationId);
        }

        var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        var cachedLocation = cachedLocations?
            .FirstOrDefault(location => string.Equals(location.Id, locationId, StringComparison.OrdinalIgnoreCase));
        if (cachedLocation is not null)
        {
            return ContentLocalizationMapper.LocalizeAudioGuides(cachedLocation.AudioGuides, GetCurrentLanguageCode(), locationId);
        }

        return await _fallback.GetAudioGuidesForLocationAsync(locationId);
    }

    public async Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId)
    {
        var remote = await TryGetAsync<AudioGuide>(WithLanguage($"api/mobile/audio/{Uri.EscapeDataString(audioGuideId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeAudioGuides(new[] { remote }, GetCurrentLanguageCode(), remote.LocationId)
                .FirstOrDefault();
        }

        var cachedLocations = await GetCachedAsync<List<Location>>(LocationsCacheKey);
        var cachedGuide = cachedLocations?
            .SelectMany(location => location.AudioGuides)
            .FirstOrDefault(guide => string.Equals(guide.Id, audioGuideId, StringComparison.OrdinalIgnoreCase));
        if (cachedGuide is not null)
        {
            return ContentLocalizationMapper.LocalizeAudioGuides(
                    new[] { cachedGuide },
                    GetCurrentLanguageCode(),
                    cachedGuide.LocationId)
                .FirstOrDefault();
        }

        return await _fallback.GetAudioGuideByIdAsync(audioGuideId);
    }

    // Keep favorites/downloads local for now.
    public Task<bool> ToggleFavoriteAsync(string locationId) => _fallback.ToggleFavoriteAsync(locationId);
    public Task<List<Location>> GetFavoriteLocationsAsync() => _fallback.GetFavoriteLocationsAsync();

    public async Task<List<ListeningHistory>> GetListeningHistoryAsync()
    {
        var remote = await TryGetAsync<List<ListeningHistory>>("api/mobile/history");
        if (remote is not null)
        {
            foreach (var item in remote)
            {
                if (item.LastListenedAt == default)
                {
                    item.LastListenedAt = item.ListenedAt == default ? DateTime.UtcNow : item.ListenedAt;
                }

                if (item.ListenedAt == default)
                {
                    item.ListenedAt = item.LastListenedAt;
                }
            }

            return remote
                .OrderByDescending(item => item.LastListenedAt)
                .ToList();
        }

        return await _fallback.GetListeningHistoryAsync();
    }

    public async Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress, int interruptedAtSeconds = 0, bool isDirectTap = false)
    {
        var request = new AddListeningHistoryRequest
        {
            AudioGuideId = audioGuideId,
            LocationId = locationId,
            Progress = progress,
            IsCompleted = progress >= 0.999,
            InterruptedAtSeconds = interruptedAtSeconds,
            IsDirectTap = isDirectTap
        };

        var posted = await TryPostAsync("api/mobile/history", request);
        if (posted)
        {
            return;
        }

        await _fallback.AddListeningHistoryAsync(audioGuideId, locationId, progress, interruptedAtSeconds, isDirectTap);
    }

    public Task<List<DownloadedAudio>> GetDownloadedAudiosAsync() => _fallback.GetDownloadedAudiosAsync();
    public Task<bool> DownloadAudioAsync(string audioGuideId) => _fallback.DownloadAudioAsync(audioGuideId);
    public Task<bool> DeleteDownloadedAudioAsync(string audioGuideId) => _fallback.DeleteDownloadedAudioAsync(audioGuideId);
    public Task<long> GetTotalDownloadSizeAsync() => _fallback.GetTotalDownloadSizeAsync();

    private async Task<T?> TryGetAsync<T>(string relativePath)
    {
        // Wrapper GET: thử base URL đang active trước, sau đó failover qua các base URL dự phòng.
        // Giúp app linh hoạt giữa môi trường local, emulator và public tunnel.
        if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
        {
            var data = await TryGetFromBaseAsync<T>(_activeBaseUrl!, relativePath);
            if (data is not null)
            {
                return data;
            }
        }

        foreach (var baseUrl in GetCandidateBaseUrls())
        {
            var data = await TryGetFromBaseAsync<T>(baseUrl, relativePath);
            if (data is not null)
            {
                _activeBaseUrl = baseUrl;
                return data;
            }
        }

        return default;
    }

    private string GetCurrentLanguageCode()
    {
        return ContentLocalizationMapper.ToLanguageCode(_localizationService.CurrentCulture.Name);
    }

    private string WithLanguage(string path)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}language={Uri.EscapeDataString(GetCurrentLanguageCode())}";
    }

    private async Task<T?> TryGetFromBaseAsync<T>(string baseUrl, string relativePath)
    {
        var requestUri = $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private async Task<bool> TryPostAsync<TBody>(string relativePath, TBody body)
    {
        // Wrapper POST không cần response body, có failover base URL tương tự GET.
        // Dùng cho các tác vụ ghi dữ liệu như history, heartbeat.
        if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
        {
            var postedToActive = await TryPostToBaseAsync(_activeBaseUrl!, relativePath, body);
            if (postedToActive)
            {
                return true;
            }
        }

        foreach (var baseUrl in GetCandidateBaseUrls())
        {
            var posted = await TryPostToBaseAsync(baseUrl, relativePath, body);
            if (posted)
            {
                _activeBaseUrl = baseUrl;
                return true;
            }
        }

        return false;
    }

    private async Task<TResponse?> TryPostAsync<TBody, TResponse>(string relativePath, TBody body)
    {
        if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
        {
            var responseFromActive = await TryPostToBaseAsync<TBody, TResponse>(_activeBaseUrl!, relativePath, body);
            if (responseFromActive is not null)
            {
                return responseFromActive;
            }
        }

        foreach (var baseUrl in GetCandidateBaseUrls())
        {
            var response = await TryPostToBaseAsync<TBody, TResponse>(baseUrl, relativePath, body);
            if (response is not null)
            {
                _activeBaseUrl = baseUrl;
                return response;
            }
        }

        return default;
    }

    private async Task<bool> TryPostToBaseAsync<TBody>(string baseUrl, string relativePath, TBody body)
    {
        var requestUri = $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<TResponse?> TryPostToBaseAsync<TBody, TResponse>(string baseUrl, string relativePath, TBody body)
    {
        var requestUri = $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";

        try
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private async Task<T?> GetCachedAsync<T>(string cacheKey)
    {
        var json = await _localDatabaseService.GetCachedJsonAsync(cacheKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private async Task UpsertCacheAsync<T>(string cacheKey, T payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await _localDatabaseService.UpsertCachedJsonAsync(cacheKey, json);
        }
        catch
        {
            // Keep cache best-effort; request should still succeed.
        }
    }

    private static List<Location> NormalizeLocations(IEnumerable<Location> locations)
    {
        return locations.Select(NormalizeLocation).ToList();
    }

    private static Location NormalizeLocation(Location location)
    {
        location.AudioGuides ??= new List<AudioGuide>();
        foreach (var guide in location.AudioGuides)
        {
            guide.ScriptSegments ??= new List<AudioScriptSegment>();
        }

        return location;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private static IReadOnlyList<string> GetCandidateBaseUrls()
    {
        var candidates = new List<string>
        {
            DefaultPublicApiBaseUrl
        };

        var preferredBaseUrl = NormalizeBaseUrl(Preferences.Get(PreferredApiBaseUrlKey, string.Empty));
        if (!string.IsNullOrWhiteSpace(preferredBaseUrl))
        {
            candidates.Insert(0, preferredBaseUrl);
        }

#if ANDROID
        candidates.AddRange(new[]
        {
            "https://10.0.2.2:7275",
            "http://10.0.2.2:5275",
            "https://localhost:7275",
            "http://localhost:5275"
        });
#else
        candidates.AddRange(new[]
        {
            "https://localhost:7275",
            "http://localhost:5275",
            "https://10.0.2.2:7275",
            "http://10.0.2.2:5275"
        });
#endif

        return candidates
            .Select(NormalizeBaseUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    private static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var trimmed = baseUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private sealed class AddListeningHistoryRequest
    {
        public string AudioGuideId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
        public int InterruptedAtSeconds { get; set; }
        public bool IsDirectTap { get; set; }
    }
}
