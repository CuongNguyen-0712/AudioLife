using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public interface IAppSessionStore
{
    Task<string> GetOrCreateDeviceIdAsync();
    Task<AppSessionSnapshot?> GetSnapshotAsync();
    Task SaveSnapshotAsync(AppSessionSnapshot snapshot);
    Task ClearSnapshotAsync();
}