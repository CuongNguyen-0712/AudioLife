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

    private static readonly string[] BaseUrls =
    {
        "https://10.0.2.2:7275",
        "http://10.0.2.2:5275",
        "https://localhost:7275",
        "http://localhost:5275"
    };

    private readonly ApiService _fallback;
    private readonly ILocalizationService _localizationService;
    private readonly HttpClient _httpClient;
    private string? _activeBaseUrl;

    public RemoteApiService(ApiService fallback, ILocalizationService localizationService)
    {
        _fallback = fallback;
        _localizationService = localizationService;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
    }

    public async Task<List<Location>> GetLocationsAsync()
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage("api/mobile/locations"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetLocationsAsync();
    }

    public async Task<Location?> GetLocationByIdAsync(string locationId)
    {
        var remote = await TryGetAsync<Location>(WithLanguage($"api/mobile/locations/{Uri.EscapeDataString(locationId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocation(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetLocationByIdAsync(locationId);
    }

    public async Task<List<Location>> SearchLocationsAsync(string query)
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/search?query={Uri.EscapeDataString(query)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(remote, GetCurrentLanguageCode());
        }

        return await _fallback.SearchLocationsAsync(query);
    }

    public async Task<List<Location>> GetLocationsByCategoryAsync(string categoryId)
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/by-category/{Uri.EscapeDataString(categoryId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetLocationsByCategoryAsync(categoryId);
    }

    public async Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1)
    {
        var remote = await TryGetAsync<List<Location>>(WithLanguage($"api/mobile/locations/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeLocations(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetNearbyLocationsAsync(latitude, longitude, radiusKm);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var remote = await TryGetAsync<List<Category>>(WithLanguage("api/mobile/categories"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeCategories(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetCategoriesAsync();
    }

    public async Task<List<Tour>> GetToursAsync()
    {
        var remote = await TryGetAsync<List<Tour>>(WithLanguage("api/mobile/tours"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeTours(remote, GetCurrentLanguageCode());
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

        return await _fallback.GetTourByIdAsync(tourId);
    }

    public async Task<List<Tour>> GetFeaturedToursAsync()
    {
        var remote = await TryGetAsync<List<Tour>>(WithLanguage("api/mobile/tours/featured"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeTours(remote, GetCurrentLanguageCode());
        }

        return await _fallback.GetFeaturedToursAsync();
    }

    public async Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId)
    {
        var remote = await TryGetAsync<List<AudioGuide>>(WithLanguage($"api/mobile/audio/by-location/{Uri.EscapeDataString(locationId)}"));
        if (remote is not null)
        {
            return ContentLocalizationMapper.LocalizeAudioGuides(remote, GetCurrentLanguageCode(), locationId);
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

    public async Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress)
    {
        var request = new AddListeningHistoryRequest
        {
            AudioGuideId = audioGuideId,
            LocationId = locationId,
            Progress = progress,
            IsCompleted = progress >= 0.999
        };

        var posted = await TryPostAsync("api/mobile/history", request);
        if (posted)
        {
            return;
        }

        await _fallback.AddListeningHistoryAsync(audioGuideId, locationId, progress);
    }

    public Task<List<DownloadedAudio>> GetDownloadedAudiosAsync() => _fallback.GetDownloadedAudiosAsync();
    public Task<bool> DownloadAudioAsync(string audioGuideId) => _fallback.DownloadAudioAsync(audioGuideId);
    public Task<bool> DeleteDownloadedAudioAsync(string audioGuideId) => _fallback.DeleteDownloadedAudioAsync(audioGuideId);
    public Task<long> GetTotalDownloadSizeAsync() => _fallback.GetTotalDownloadSizeAsync();

    private async Task<T?> TryGetAsync<T>(string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
        {
            var data = await TryGetFromBaseAsync<T>(_activeBaseUrl!, relativePath);
            if (data is not null)
            {
                return data;
            }
        }

        foreach (var baseUrl in BaseUrls)
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
        if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
        {
            var postedToActive = await TryPostToBaseAsync(_activeBaseUrl!, relativePath, body);
            if (postedToActive)
            {
                return true;
            }
        }

        foreach (var baseUrl in BaseUrls)
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

    private sealed class AddListeningHistoryRequest
    {
        public string AudioGuideId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public double Progress { get; set; }
        public bool IsCompleted { get; set; }
    }
}
