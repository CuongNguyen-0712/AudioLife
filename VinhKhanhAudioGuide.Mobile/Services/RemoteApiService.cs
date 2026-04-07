using System.Text.Json;
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
    private readonly HttpClient _httpClient;
    private string? _activeBaseUrl;

    public RemoteApiService(ApiService fallback)
    {
        _fallback = fallback;

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
        => await TryGetAsync<List<Location>>("api/mobile/locations") ?? await _fallback.GetLocationsAsync();

    public async Task<Location?> GetLocationByIdAsync(string locationId)
        => await TryGetAsync<Location>($"api/mobile/locations/{Uri.EscapeDataString(locationId)}")
           ?? await _fallback.GetLocationByIdAsync(locationId);

    public async Task<List<Location>> SearchLocationsAsync(string query)
        => await TryGetAsync<List<Location>>($"api/mobile/locations/search?query={Uri.EscapeDataString(query)}")
           ?? await _fallback.SearchLocationsAsync(query);

    public async Task<List<Location>> GetLocationsByCategoryAsync(string categoryId)
        => await TryGetAsync<List<Location>>($"api/mobile/locations/by-category/{Uri.EscapeDataString(categoryId)}")
           ?? await _fallback.GetLocationsByCategoryAsync(categoryId);

    public async Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1)
        => await TryGetAsync<List<Location>>($"api/mobile/locations/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}")
           ?? await _fallback.GetNearbyLocationsAsync(latitude, longitude, radiusKm);

    public async Task<List<Category>> GetCategoriesAsync()
        => await TryGetAsync<List<Category>>("api/mobile/categories") ?? await _fallback.GetCategoriesAsync();

    public async Task<List<Tour>> GetToursAsync()
        => await TryGetAsync<List<Tour>>("api/mobile/tours") ?? await _fallback.GetToursAsync();

    public async Task<Tour?> GetTourByIdAsync(string tourId)
        => await TryGetAsync<Tour>($"api/mobile/tours/{Uri.EscapeDataString(tourId)}")
           ?? await _fallback.GetTourByIdAsync(tourId);

    public async Task<List<Tour>> GetFeaturedToursAsync()
        => await TryGetAsync<List<Tour>>("api/mobile/tours/featured") ?? await _fallback.GetFeaturedToursAsync();

    public async Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId)
        => await TryGetAsync<List<AudioGuide>>($"api/mobile/audio/by-location/{Uri.EscapeDataString(locationId)}")
           ?? await _fallback.GetAudioGuidesForLocationAsync(locationId);

    public async Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId)
        => await TryGetAsync<AudioGuide>($"api/mobile/audio/{Uri.EscapeDataString(audioGuideId)}")
           ?? await _fallback.GetAudioGuideByIdAsync(audioGuideId);

    // Keep user-specific features local for now.
    public Task<UserProfile?> GetUserProfileAsync() => _fallback.GetUserProfileAsync();
    public Task<bool> UpdateUserProfileAsync(UserProfile profile) => _fallback.UpdateUserProfileAsync(profile);
    public Task<bool> ToggleFavoriteAsync(string locationId) => _fallback.ToggleFavoriteAsync(locationId);
    public Task<List<Location>> GetFavoriteLocationsAsync() => _fallback.GetFavoriteLocationsAsync();
    public Task<List<ListeningHistory>> GetListeningHistoryAsync() => _fallback.GetListeningHistoryAsync();
    public Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress)
        => _fallback.AddListeningHistoryAsync(audioGuideId, locationId, progress);
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
}
