using VinhKhanhAudioGuide.Mobile.Models;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// API service interface for communicating with the backend.
/// Currently uses local sample data; swap implementation for real API calls.
/// </summary>
public interface IApiService
{
    // Locations
    Task<List<Location>> GetLocationsAsync();
    Task<Location?> GetLocationByIdAsync(string locationId);
    Task<List<Location>> SearchLocationsAsync(string query);
    Task<List<Location>> GetLocationsByCategoryAsync(string categoryId);
    Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1);

    // Categories
    Task<List<Category>> GetCategoriesAsync();

    // Tours
    Task<List<Tour>> GetToursAsync();
    Task<Tour?> GetTourByIdAsync(string tourId);
    Task<List<Tour>> GetFeaturedToursAsync();

    // Audio
    Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId);
    Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId);

    // Favorites
    Task<bool> ToggleFavoriteAsync(string locationId);
    Task<List<Location>> GetFavoriteLocationsAsync();

    // History
    Task<List<ListeningHistory>> GetListeningHistoryAsync();
    Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress);

    // Downloads
    Task<List<DownloadedAudio>> GetDownloadedAudiosAsync();
    Task<bool> DownloadAudioAsync(string audioGuideId);
    Task<bool> DeleteDownloadedAudioAsync(string audioGuideId);
    Task<long> GetTotalDownloadSizeAsync();
}
