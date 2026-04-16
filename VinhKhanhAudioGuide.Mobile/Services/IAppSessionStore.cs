namespace VinhKhanhAudioGuide.Mobile.Services;

public interface IAppSessionStore
{
    Task<string> GetOrCreateDeviceIdAsync();
}