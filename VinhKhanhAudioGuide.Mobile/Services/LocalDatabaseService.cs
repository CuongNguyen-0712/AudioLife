using System.Text.Json;
using SQLite;
using VinhKhanhAudioGuide.Mobile.Models;

namespace VinhKhanhAudioGuide.Mobile.Services;

public class LocalDatabaseService : ILocalDatabaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SQLiteAsyncConnection _database;

    public LocalDatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "mobile_local.db3");
        _database = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    private async Task EnsureInitializedAsync()
    {
        await _database.CreateTableAsync<UserProfileEntity>();
        await _database.CreateTableAsync<ListeningHistoryEntity>();
        await _database.CreateTableAsync<DownloadedAudioEntity>();
    }

    public async Task<UserProfile?> GetUserProfileAsync()
    {
        await EnsureInitializedAsync();
        var entity = await _database.Table<UserProfileEntity>().FirstOrDefaultAsync();
        return entity is null ? null : ToModel(entity);
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(ToEntity(profile));
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
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(ToEntity(download));
    }

    public async Task DeleteDownloadedAudioAsync(string audioGuideId)
    {
        await EnsureInitializedAsync();
        await _database.DeleteAsync<DownloadedAudioEntity>(audioGuideId);
    }

    private static UserProfileEntity ToEntity(UserProfile model)
    {
        return new UserProfileEntity
        {
            Id = model.Id,
            Name = model.Name,
            Email = model.Email,
            AvatarUrl = model.AvatarUrl,
            PreferredLanguage = model.PreferredLanguage,
            FavoriteLocationIdsJson = JsonSerializer.Serialize(model.FavoriteLocationIds, JsonOptions),
            VisitedLocationIdsJson = JsonSerializer.Serialize(model.VisitedLocationIds, JsonOptions),
            CreatedAtUtcTicks = model.CreatedAt.ToUniversalTime().Ticks,
            LastLoginAtUtcTicks = model.LastLoginAt.ToUniversalTime().Ticks
        };
    }

    private static UserProfile ToModel(UserProfileEntity entity)
    {
        return new UserProfile
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            AvatarUrl = entity.AvatarUrl,
            PreferredLanguage = entity.PreferredLanguage,
            FavoriteLocationIds = DeserializeStringList(entity.FavoriteLocationIdsJson),
            VisitedLocationIds = DeserializeStringList(entity.VisitedLocationIdsJson),
            CreatedAt = new DateTime(entity.CreatedAtUtcTicks, DateTimeKind.Utc).ToLocalTime(),
            LastLoginAt = new DateTime(entity.LastLoginAtUtcTicks, DateTimeKind.Utc).ToLocalTime()
        };
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

    private static List<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private sealed class UserProfileEntity
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "vi";
        public string FavoriteLocationIdsJson { get; set; } = "[]";
        public string VisitedLocationIdsJson { get; set; } = "[]";
        public long CreatedAtUtcTicks { get; set; }
        public long LastLoginAtUtcTicks { get; set; }
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
}
