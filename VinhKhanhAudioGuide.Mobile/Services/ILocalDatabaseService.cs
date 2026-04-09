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
}
