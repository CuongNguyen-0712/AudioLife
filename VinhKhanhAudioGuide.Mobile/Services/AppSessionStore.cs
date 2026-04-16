using Microsoft.Maui.Storage;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class AppSessionStore : IAppSessionStore
{
    private const string DeviceIdKey = "app.session.device-id";

    public Task<string> GetOrCreateDeviceIdAsync()
    {
        var deviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return Task.FromResult(deviceId);
        }

        deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdKey, deviceId);
        return Task.FromResult(deviceId);
    }
}