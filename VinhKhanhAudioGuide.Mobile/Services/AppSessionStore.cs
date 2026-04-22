using System.Text.Json;
using Microsoft.Maui.Storage;
using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class AppSessionStore : IAppSessionStore
{
    private const string DeviceIdKey = "app.session.device-id";
    private const string SnapshotKey = "app.session.snapshot";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public Task<AppSessionSnapshot?> GetSnapshotAsync()
    {
        var json = Preferences.Default.Get(SnapshotKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<AppSessionSnapshot?>(null);
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<AppSessionSnapshot>(json, JsonOptions);
            return Task.FromResult<AppSessionSnapshot?>(snapshot);
        }
        catch
        {
            return Task.FromResult<AppSessionSnapshot?>(null);
        }
    }

    public Task SaveSnapshotAsync(AppSessionSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        Preferences.Default.Set(SnapshotKey, json);
        Preferences.Default.Set("app.session.session-token", snapshot.SessionToken);
        return Task.CompletedTask;
    }

    public Task ClearSnapshotAsync()
    {
        Preferences.Default.Remove(SnapshotKey);
        Preferences.Default.Remove("app.session.session-token");
        return Task.CompletedTask;
    }
}