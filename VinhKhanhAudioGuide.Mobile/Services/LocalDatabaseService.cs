using SQLite;
using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class LocalDatabaseService : ILocalDatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public LocalDatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "mobile_local.db3");
        _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    private async Task EnsureInitializedAsync()
    {
        await _database.CreateTableAsync<ListeningHistoryEntity>();
        await _database.CreateTableAsync<DownloadedAudioEntity>();
        await _database.CreateTableAsync<CachedJsonEntity>();
        await _database.CreateTableAsync<PlaybackQueueEntity>();
        await _database.CreateTableAsync<LocationPlaybackHistoryEntity>();
        await _database.CreateTableAsync<FavoriteLocationEntity>();
    }

    public async Task<List<string>> GetFavoriteIdsAsync()
    {
        await EnsureInitializedAsync();
        var favorites = await _database.Table<FavoriteLocationEntity>().ToListAsync();
        return favorites.Select(f => f.LocationId).ToList();
    }

    public async Task<bool> IsFavoriteAsync(string locationId)
    {
        if (string.IsNullOrEmpty(locationId)) return false;
        await EnsureInitializedAsync();
        var entity = await _database.Table<FavoriteLocationEntity>()
            .FirstOrDefaultAsync(f => f.LocationId == locationId);
        return entity != null;
    }

    public async Task<bool> ToggleFavoriteAsync(string locationId)
    {
        if (string.IsNullOrEmpty(locationId)) return false;
        await EnsureInitializedAsync();

        var existing = await _database.Table<FavoriteLocationEntity>()
            .FirstOrDefaultAsync(f => f.LocationId == locationId);

        if (existing != null)
        {
            await _database.DeleteAsync(existing);
            return false;
        }
        else
        {
            await _database.InsertAsync(new FavoriteLocationEntity { LocationId = locationId });
            return true;
        }
    }

    public async Task<List<ListeningHistory>> GetListeningHistoryAsync()
    {
        await EnsureInitializedAsync();
        var entities = await _database.Table<ListeningHistoryEntity>().ToListAsync();
        return entities
            .Select(ToModel)
            .OrderByDescending(x => x.ListenedAt)
            .ToList();
    }

    public async Task UpsertListeningHistoryAsync(ListeningHistory history)
    {
        // Upsert lịch sử nghe để lưu tiến độ nghe gần nhất theo từng audio.
        // Thuộc flow player + resume lịch sử nghe.
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(ToEntity(history));
    }

    public async Task<List<DownloadedAudio>> GetDownloadedAudiosAsync()
    {
        await EnsureInitializedAsync();
        var entities = await _database.Table<DownloadedAudioEntity>().ToListAsync();
        return entities
            .Select(ToModel)
            .OrderByDescending(x => x.DownloadedAt)
            .ToList();
    }

    public async Task UpsertDownloadedAudioAsync(DownloadedAudio download)
    {
        // Lưu metadata file audio đã tải về để hỗ trợ offline playback.
        // Thuộc flow download/manage dữ liệu offline.
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(ToEntity(download));
    }

    public async Task DeleteDownloadedAudioAsync(string audioGuideId)
    {
        await EnsureInitializedAsync();
        await _database.DeleteAsync<DownloadedAudioEntity>(audioGuideId);
    }

    public async Task<string?> GetCachedJsonAsync(string cacheKey)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return null;
        }

        var entity = await _database.Table<CachedJsonEntity>()
            .FirstOrDefaultAsync(item => item.CacheKey == cacheKey);

        return entity?.JsonPayload;
    }

    public async Task UpsertCachedJsonAsync(string cacheKey, string jsonPayload)
    {
        // Cache JSON theo key (categories/locations/tours) để fallback khi mất mạng.
        // Thuộc flow remote -> cache -> local sample data.
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(cacheKey) || string.IsNullOrWhiteSpace(jsonPayload))
        {
            return;
        }

        var entity = new CachedJsonEntity
        {
            CacheKey = cacheKey,
            JsonPayload = jsonPayload,
            UpdatedAtUtcTicks = DateTime.UtcNow.Ticks
        };

        await _database.InsertOrReplaceAsync(entity);
    }

    public async Task EnqueuePlaybackAsync(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId)) return;
        await EnsureInitializedAsync();
        
        var entity = new PlaybackQueueEntity
        {
            LocationId = locationId,
            EnqueuedAtUtcTicks = DateTime.UtcNow.Ticks
        };
        await _database.InsertAsync(entity);
    }

    public async Task<string?> DequeuePlaybackAsync()
    {
        await EnsureInitializedAsync();
        var entity = await _database.Table<PlaybackQueueEntity>()
            .OrderBy(x => x.EnqueuedAtUtcTicks)
            .FirstOrDefaultAsync();

        if (entity != null)
        {
            await _database.DeleteAsync(entity);
            return entity.LocationId;
        }

        return null;
    }

    public async Task ClearPlaybackQueueAsync()
    {
        await EnsureInitializedAsync();
        await _database.DeleteAllAsync<PlaybackQueueEntity>();
    }

    public async Task<bool> IsInPlaybackQueueAsync(string locationId)
    {
        await EnsureInitializedAsync();
        var count = await _database.Table<PlaybackQueueEntity>()
            .Where(x => x.LocationId == locationId)
            .CountAsync();
        return count > 0;
    }

    public async Task<DateTime?> GetLastPlayedAtAsync(string locationId)
    {
        await EnsureInitializedAsync();
        var entity = await _database.Table<LocationPlaybackHistoryEntity>()
            .FirstOrDefaultAsync(x => x.LocationId == locationId);
            
        if (entity == null) return null;
        return new DateTime(entity.LastPlayedAtUtcTicks, DateTimeKind.Utc);
    }

    public async Task SetLastPlayedAtAsync(string locationId, DateTime time)
    {
        await EnsureInitializedAsync();
        var entity = new LocationPlaybackHistoryEntity
        {
            LocationId = locationId,
            LastPlayedAtUtcTicks = time.ToUniversalTime().Ticks
        };
        await _database.InsertOrReplaceAsync(entity);
    }

    private static ListeningHistoryEntity ToEntity(ListeningHistory model)
    {
        return new ListeningHistoryEntity
        {
            Id = model.Id,
            AudioGuideId = model.AudioGuideId,
            AudioTitle = model.AudioTitle,
            LocationId = model.LocationId,
            LocationName = model.LocationName,
            LocationImageUrl = model.LocationImageUrl,
            AudioDuration = model.AudioDuration,
            Progress = model.Progress,
            ListenedAtUtcTicks = model.ListenedAt.ToUniversalTime().Ticks,
            UserId = model.UserId,
            ListenedSeconds = model.ListenedSeconds,
            IsCompleted = model.IsCompleted,
            LastListenedAtUtcTicks = model.LastListenedAt.ToUniversalTime().Ticks,
            InterruptedAtSeconds = model.InterruptedAtSeconds,
            IsDirectTap = model.IsDirectTap
        };
    }

    private static ListeningHistory ToModel(ListeningHistoryEntity entity)
    {
        return new ListeningHistory
        {
            Id = entity.Id,
            AudioGuideId = entity.AudioGuideId,
            AudioTitle = entity.AudioTitle,
            LocationId = entity.LocationId,
            LocationName = entity.LocationName,
            LocationImageUrl = entity.LocationImageUrl,
            AudioDuration = entity.AudioDuration,
            Progress = entity.Progress,
            ListenedAt = new DateTime(entity.ListenedAtUtcTicks, DateTimeKind.Utc).ToLocalTime(),
            UserId = entity.UserId,
            ListenedSeconds = entity.ListenedSeconds,
            IsCompleted = entity.IsCompleted,
            LastListenedAt = new DateTime(entity.LastListenedAtUtcTicks, DateTimeKind.Utc).ToLocalTime(),
            InterruptedAtSeconds = entity.InterruptedAtSeconds,
            IsDirectTap = entity.IsDirectTap
        };
    }

    private static DownloadedAudioEntity ToEntity(DownloadedAudio model)
    {
        return new DownloadedAudioEntity
        {
            AudioGuideId = model.AudioGuideId,
            LocalPath = model.LocalPath,
            DownloadedAtUtcTicks = model.DownloadedAt.ToUniversalTime().Ticks,
            FileSize = model.FileSize
        };
    }

    private static DownloadedAudio ToModel(DownloadedAudioEntity entity)
    {
        return new DownloadedAudio
        {
            AudioGuideId = entity.AudioGuideId,
            LocalPath = entity.LocalPath,
            DownloadedAt = new DateTime(entity.DownloadedAtUtcTicks, DateTimeKind.Utc).ToLocalTime(),
            FileSize = entity.FileSize
        };
    }

    private sealed class ListeningHistoryEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        public string AudioGuideId { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationImageUrl { get; set; } = string.Empty;
        public int AudioDuration { get; set; }
        public double Progress { get; set; }
        public long ListenedAtUtcTicks { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ListenedSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public long LastListenedAtUtcTicks { get; set; }
        public int InterruptedAtSeconds { get; set; }
        public bool IsDirectTap { get; set; }
    }

    private sealed class DownloadedAudioEntity
    {
        [PrimaryKey]
        public string AudioGuideId { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public long DownloadedAtUtcTicks { get; set; }
        public long FileSize { get; set; }
    }

    private sealed class CachedJsonEntity
    {
        [PrimaryKey]
        public string CacheKey { get; set; } = string.Empty;
        public string JsonPayload { get; set; } = string.Empty;
        public long UpdatedAtUtcTicks { get; set; }
    }

    private sealed class PlaybackQueueEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string LocationId { get; set; } = string.Empty;
        public long EnqueuedAtUtcTicks { get; set; }
    }

    private sealed class LocationPlaybackHistoryEntity
    {
        [PrimaryKey]
        public string LocationId { get; set; } = string.Empty;
        public long LastPlayedAtUtcTicks { get; set; }
    }

    private sealed class FavoriteLocationEntity
    {
        [PrimaryKey]
        public string LocationId { get; set; } = string.Empty;
    }
}
