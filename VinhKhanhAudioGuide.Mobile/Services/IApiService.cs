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

    // Payments / Session
    Task<List<PaymentPackage>> GetPaymentPackagesAsync();
    Task<DeviceSessionCheckResult?> CheckDeviceSessionAsync(string deviceId);
    Task<PaymentCompletionResult?> CompletePaymentAsync(PaymentCompletionRequest request);
    Task<SessionValidationResult?> ValidateSessionAsync(string sessionToken, string deviceId);
    Task<HeartbeatResponse?> SendHeartbeatAsync(HeartbeatRequest request);
    Task<bool> TestServerConnectionAsync();

    // Audio
    Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId);
    Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId);



    // History
    Task<List<ListeningHistory>> GetListeningHistoryAsync();
    Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress, int interruptedAtSeconds = 0, bool isDirectTap = false);

    // Downloads
    Task<List<DownloadedAudio>> GetDownloadedAudiosAsync();
    Task<bool> DownloadAudioAsync(string audioGuideId);
    Task<bool> DeleteDownloadedAudioAsync(string audioGuideId);
    Task<long> GetTotalDownloadSizeAsync();

    // Favorites
    Task<List<Location>> GetFavoriteLocationsAsync();
    Task<bool> ToggleFavoriteAsync(string locationId);
    // Reviews
    Task<List<MobileLocationReviewDto>> GetLocationReviewsAsync(string locationId);
    Task<bool> SubmitLocationReviewAsync(string locationId, SubmitReviewRequest request);
}

