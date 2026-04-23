using SQLite;
using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class LocalDatabaseService : ILocalDatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public LocalDatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "mobile_local.db3");
        _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        if (_isInitialized)
        {
            _initLock.Release();
            return;
        }

        try
        {
        await _database.CreateTableAsync<FavoriteLocationEntity>();
        await _database.CreateTableAsync<ListeningHistoryEntity>();
        await _database.CreateTableAsync<DownloadedAudioEntity>();
        await _database.CreateTableAsync<CachedJsonEntity>();

            await CreateIndexesAsync();
            await CleanupInvalidRowsAsync();

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<string>> GetFavoriteLocationIdsAsync()
    {
        await EnsureInitializedAsync();
        var entities = await _database.Table<FavoriteLocationEntity>().ToListAsync();
        return entities.Select(x => x.LocationId).ToList();
    }

    public async Task SaveFavoriteLocationIdsAsync(IReadOnlyCollection<string> locationIds)
    {
        await EnsureInitializedAsync();

        var distinctIds = locationIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await _database.RunInTransactionAsync(connection =>
        {
            connection.DeleteAll<FavoriteLocationEntity>();
            if (distinctIds.Count > 0)
            {
                var entities = distinctIds
                    .Select(id => new FavoriteLocationEntity { LocationId = id })
                    .ToList();
                connection.InsertAll(entities);
            }
        });
    }

    public async Task<List<ListeningHistory>> GetListeningHistoryAsync()
    {
        await EnsureInitializedAsync();
        var entities = await _database.QueryAsync<ListeningHistoryEntity>(
            "SELECT * FROM ListeningHistoryEntity ORDER BY LastListenedAtUtcTicks DESC, ListenedAtUtcTicks DESC");
        return entities
            .Select(ToModel)
            .ToList();
    }

    public async Task UpsertListeningHistoryAsync(ListeningHistory history)
    {
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(ToEntity(history));
    }

    public async Task<List<DownloadedAudio>> GetDownloadedAudiosAsync()
    {
        await EnsureInitializedAsync();
        var entities = await _database.QueryAsync<DownloadedAudioEntity>(
            "SELECT * FROM DownloadedAudioEntity ORDER BY DownloadedAtUtcTicks DESC");
        return entities
            .Select(ToModel)
            .ToList();
    }

    public async Task UpsertDownloadedAudioAsync(DownloadedAudio download)
    {
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

    private async Task CreateIndexesAsync()
    {
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_history_last_listened ON ListeningHistoryEntity(LastListenedAtUtcTicks DESC)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_history_audio_guide ON ListeningHistoryEntity(AudioGuideId)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_downloaded_at ON DownloadedAudioEntity(DownloadedAtUtcTicks DESC)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_cache_updated_at ON CachedJsonEntity(UpdatedAtUtcTicks DESC)");
    }

    private async Task CleanupInvalidRowsAsync()
    {
        await _database.ExecuteAsync("DELETE FROM FavoriteLocationEntity WHERE LocationId IS NULL OR TRIM(LocationId) = ''");
        await _database.ExecuteAsync("DELETE FROM ListeningHistoryEntity WHERE Id IS NULL OR TRIM(Id) = '' OR AudioGuideId IS NULL OR TRIM(AudioGuideId) = '' OR LocationId IS NULL OR TRIM(LocationId) = ''");
        await _database.ExecuteAsync("DELETE FROM DownloadedAudioEntity WHERE AudioGuideId IS NULL OR TRIM(AudioGuideId) = '' OR LocalPath IS NULL OR TRIM(LocalPath) = ''");
        await _database.ExecuteAsync("DELETE FROM CachedJsonEntity WHERE CacheKey IS NULL OR TRIM(CacheKey) = '' OR JsonPayload IS NULL OR TRIM(JsonPayload) = ''");
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
            LastListenedAtUtcTicks = model.LastListenedAt.ToUniversalTime().Ticks
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
            LastListenedAt = new DateTime(entity.LastListenedAtUtcTicks, DateTimeKind.Utc).ToLocalTime()
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

    private sealed class FavoriteLocationEntity
    {
        [PrimaryKey]
        public string LocationId { get; set; } = string.Empty;
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
}
