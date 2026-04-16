using System.Text.Json;

namespace VinhKhanhAudioGuide.Mobile.Services;

public sealed class TourCheckpoint
{
    public string TourId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string AudioGuideId { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public double AudioPositionSeconds { get; set; }
    public DateTime SavedAtUtc { get; set; }
}

public interface ITourCheckpointService
{
    Task<TourCheckpoint?> GetAsync();
    Task SaveAsync(TourCheckpoint checkpoint);
    Task ClearAsync();
}

public sealed class TourCheckpointService : ITourCheckpointService
{
    private const string TourCheckpointPreferenceKey = "TourCheckpoint";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<TourCheckpoint?> GetAsync()
    {
        var raw = Preferences.Get(TourCheckpointPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Task.FromResult<TourCheckpoint?>(null);
        }

        try
        {
            var checkpoint = JsonSerializer.Deserialize<TourCheckpoint>(raw, JsonOptions);
            return Task.FromResult(checkpoint);
        }
        catch
        {
            Preferences.Remove(TourCheckpointPreferenceKey);
            return Task.FromResult<TourCheckpoint?>(null);
        }
    }

    public Task SaveAsync(TourCheckpoint checkpoint)
    {
        var raw = JsonSerializer.Serialize(checkpoint, JsonOptions);
        Preferences.Set(TourCheckpointPreferenceKey, raw);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Remove(TourCheckpointPreferenceKey);
        return Task.CompletedTask;
    }
}
