using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public interface ILocalDatabaseService
{
    Task<List<string>> GetFavoriteLocationIdsAsync();
    Task SaveFavoriteLocationIdsAsync(IReadOnlyCollection<string> locationIds);

    Task<List<ListeningHistory>> GetListeningHistoryAsync();
    Task UpsertListeningHistoryAsync(ListeningHistory history);

    Task<List<DownloadedAudio>> GetDownloadedAudiosAsync();
    Task UpsertDownloadedAudioAsync(DownloadedAudio download);
    Task DeleteDownloadedAudioAsync(string audioGuideId);

    Task<string?> GetCachedJsonAsync(string cacheKey);
    Task UpsertCachedJsonAsync(string cacheKey, string jsonPayload);

    Task EnqueuePlaybackAsync(string locationId);
    Task<string?> DequeuePlaybackAsync();
    Task ClearPlaybackQueueAsync();
    Task<bool> IsInPlaybackQueueAsync(string locationId);

    Task<DateTime?> GetLastPlayedAtAsync(string locationId);
    Task SetLastPlayedAtAsync(string locationId, DateTime time);
}
