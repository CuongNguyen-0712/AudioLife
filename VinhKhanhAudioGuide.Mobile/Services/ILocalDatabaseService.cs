using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public interface ILocalDatabaseService
{
    Task<UserProfile?> GetUserProfileAsync();
    Task SaveUserProfileAsync(UserProfile profile);

    Task<List<ListeningHistory>> GetListeningHistoryAsync();
    Task UpsertListeningHistoryAsync(ListeningHistory history);

    Task<List<DownloadedAudio>> GetDownloadedAudiosAsync();
    Task UpsertDownloadedAudioAsync(DownloadedAudio download);
    Task DeleteDownloadedAudioAsync(string audioGuideId);
}
