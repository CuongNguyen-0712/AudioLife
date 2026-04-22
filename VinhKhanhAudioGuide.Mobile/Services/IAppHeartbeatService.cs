namespace VinhKhanhAudioGuide.Mobile.Services;

public interface IAppHeartbeatService
{
    bool IsRunning { get; }
    Task<bool> StartAsync(Func<Task>? onSessionInvalidated = null);
    Task StopAsync();
}