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
        // Lấy định danh thiết bị ổn định, nếu chưa có thì tạo mới và lưu Preferences.
        // Dùng trong flow session/device binding khi gọi API auth.
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
        // Đọc snapshot session local để biết user có thể vào app ngay hay phải login/scan lại.
        // Thuộc flow startup offline-first.
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
        // Lưu session token + metadata mới sau validate/payment/heartbeat.
        // Là state persistence chính cho authentication trên mobile.
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        Preferences.Default.Set(SnapshotKey, json);
        Preferences.Default.Set("app.session.session-token", snapshot.SessionToken);
        return Task.CompletedTask;
    }

    public Task ClearSnapshotAsync()
    {
        // Xóa toàn bộ session local khi logout hoặc session invalid.
        // Thuộc flow bảo mật và reset trạng thái user.
        Preferences.Default.Remove(SnapshotKey);
        Preferences.Default.Remove("app.session.session-token");
        return Task.CompletedTask;
    }
}