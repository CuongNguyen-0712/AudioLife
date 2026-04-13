using VinhKhanhAudioGuide.Mobile.Data;
using VinhKhanhAudioGuide.Mobile.Models;
using Location = VinhKhanhAudioGuide.Mobile.Models.Location;

namespace VinhKhanhAudioGuide.Mobile.Services;

/// <summary>
/// API service implementation using local sample data.
/// Replace with HTTP calls (HttpClient) when backend is ready.
/// </summary>
public class ApiService : IApiService
{
    private static readonly HttpClient HttpClient = new();
    private static readonly string DownloadDirectory = Path.Combine(FileSystem.AppDataDirectory, "downloads", "audio");
    private const string LocalUserId = "local_user";

    private readonly ILocalDatabaseService _localDatabaseService;
    private readonly ILocalizationService _localizationService;
    private readonly List<Location> _locations;
    private readonly List<Category> _categories;
    private readonly List<Tour> _tours;
    private readonly HashSet<string> _favoriteLocationIds;
    private readonly List<ListeningHistory> _history;
    private readonly List<DownloadedAudio> _downloads;
    private readonly SemaphoreSlim _localDataSyncLock = new(1, 1);
    private bool _localDataLoaded;

    public ApiService(ILocalDatabaseService localDatabaseService, ILocalizationService localizationService)
    {
        _localDatabaseService = localDatabaseService;
        _localizationService = localizationService;
        _locations = SampleData.GetLocations();
        _categories = SampleData.GetCategories();
        _tours = SampleData.GetTours();
        _favoriteLocationIds = CreateDefaultFavoriteLocationIds();
        _history = CreateDefaultHistory();
        _downloads = new List<DownloadedAudio>();
    }

    // ──────── Locations ────────

    public Task<List<Location>> GetLocationsAsync()
    {
        var localized = ContentLocalizationMapper.LocalizeLocations(_locations, GetCurrentLanguageCode(), _favoriteLocationIds);
        return Task.FromResult(localized);
    }

    public Task<Location?> GetLocationByIdAsync(string locationId)
    {
        var source = _locations.FirstOrDefault(l => l.Id == locationId);
        var localized = source is null
            ? null
            : ContentLocalizationMapper.LocalizeLocation(source, GetCurrentLanguageCode(), _favoriteLocationIds);

        return Task.FromResult(localized);
    }

    public Task<List<Location>> SearchLocationsAsync(string query)
    {
        var q = query.ToLowerInvariant();
        var localizedLocations = ContentLocalizationMapper.LocalizeLocations(_locations, GetCurrentLanguageCode(), _favoriteLocationIds);
        var results = localizedLocations.Where(l =>
            l.Name.Contains(q, StringComparison.InvariantCultureIgnoreCase) ||
            l.Description.Contains(q, StringComparison.InvariantCultureIgnoreCase) ||
            l.Address.Contains(q, StringComparison.InvariantCultureIgnoreCase)
        ).ToList();
        return Task.FromResult(results);
    }

    public Task<List<Location>> GetLocationsByCategoryAsync(string categoryId)
    {
        var localizedLocations = ContentLocalizationMapper.LocalizeLocations(_locations, GetCurrentLanguageCode(), _favoriteLocationIds);
        return Task.FromResult(localizedLocations.Where(l => l.CategoryId == categoryId).ToList());
    }

    public Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1)
    {
        var localizedLocations = ContentLocalizationMapper.LocalizeLocations(_locations, GetCurrentLanguageCode(), _favoriteLocationIds);
        var nearby = localizedLocations.Where(l =>
        {
            var distance = CalculateDistance(latitude, longitude, l.Latitude, l.Longitude);
            return distance <= radiusKm;
        }).OrderBy(l => CalculateDistance(latitude, longitude, l.Latitude, l.Longitude)).ToList();

        return Task.FromResult(nearby);
    }

    // ──────── Categories ────────

    public Task<List<Category>> GetCategoriesAsync()
    {
        var localized = ContentLocalizationMapper.LocalizeCategories(_categories, GetCurrentLanguageCode());
        return Task.FromResult(localized);
    }

    // ──────── Tours ────────

    public Task<List<Tour>> GetToursAsync()
    {
        var localized = ContentLocalizationMapper.LocalizeTours(_tours, GetCurrentLanguageCode());
        return Task.FromResult(localized);
    }

    public Task<Tour?> GetTourByIdAsync(string tourId)
    {
        var source = _tours.FirstOrDefault(t => t.Id == tourId);
        var localized = source is null
            ? null
            : ContentLocalizationMapper.LocalizeTours(new[] { source }, GetCurrentLanguageCode()).First();

        return Task.FromResult(localized);
    }

    public Task<List<Tour>> GetFeaturedToursAsync()
    {
        var localized = ContentLocalizationMapper.LocalizeTours(_tours.Where(t => t.IsFeatured), GetCurrentLanguageCode());
        return Task.FromResult(localized);
    }

    // ──────── Audio ────────

    public Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId)
    {
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (location is null)
        {
            return Task.FromResult(new List<AudioGuide>());
        }

        var localizedGuides = ContentLocalizationMapper.LocalizeAudioGuides(location.AudioGuides, GetCurrentLanguageCode(), location.Id);
        return Task.FromResult(localizedGuides);
    }

    public Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId)
    {
        var languageCode = GetCurrentLanguageCode();
        var audio = _locations
            .SelectMany(location => ContentLocalizationMapper.LocalizeAudioGuides(location.AudioGuides, languageCode, location.Id))
            .FirstOrDefault(guide => guide.Id == audioGuideId);

        return Task.FromResult(audio);
    }

    // ──────── Favorites ────────

    public async Task<bool> ToggleFavoriteAsync(string locationId)
    {
        await EnsureLocalDataLoadedAsync();
        if (_favoriteLocationIds.Contains(locationId))
            _favoriteLocationIds.Remove(locationId);
        else
            _favoriteLocationIds.Add(locationId);

        await _localDatabaseService.SaveFavoriteLocationIdsAsync(_favoriteLocationIds.ToList());
        return true;
    }

    public async Task<List<Location>> GetFavoriteLocationsAsync()
    {
        await EnsureLocalDataLoadedAsync();
        var localizedLocations = ContentLocalizationMapper.LocalizeLocations(_locations, GetCurrentLanguageCode(), _favoriteLocationIds);
        var favorites = localizedLocations.Where(l => _favoriteLocationIds.Contains(l.Id)).ToList();
        return favorites;
    }

    // ──────── History ────────

    public async Task<List<ListeningHistory>> GetListeningHistoryAsync()
    {
        await EnsureLocalDataLoadedAsync();
        return _history.OrderByDescending(h => h.ListenedAt).ToList();
    }

    private string GetCurrentLanguageCode()
    {
        return ContentLocalizationMapper.ToLanguageCode(_localizationService.CurrentCulture.Name);
    }

    public async Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress)
    {
        await EnsureLocalDataLoadedAsync();
        var audio = _locations.SelectMany(l => l.AudioGuides).FirstOrDefault(a => a.Id == audioGuideId);
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (audio != null && location != null)
        {
            var existing = _history.FirstOrDefault(h => h.AudioGuideId == audioGuideId);
            if (existing != null)
            {
                existing.Progress = progress;
                existing.ListenedAt = DateTime.Now;
                existing.ListenedSeconds = (int)Math.Round(audio.Duration * 60 * progress);
                existing.LastListenedAt = existing.ListenedAt;
                existing.IsCompleted = progress >= 0.999;
                await _localDatabaseService.UpsertListeningHistoryAsync(existing);
            }
            else
            {
                var item = new ListeningHistory
                {
                    Id = $"h{_history.Count + 1}",
                    AudioGuideId = audioGuideId,
                    AudioTitle = audio.Title,
                    LocationId = locationId,
                    LocationName = location.Name,
                    LocationImageUrl = location.ImageUrl,
                    AudioDuration = audio.Duration,
                    Progress = progress,
                    ListenedAt = DateTime.Now,
                    UserId = LocalUserId,
                    ListenedSeconds = (int)Math.Round(audio.Duration * 60 * progress),
                    LastListenedAt = DateTime.Now,
                    IsCompleted = progress >= 0.999
                };
                _history.Add(item);
                await _localDatabaseService.UpsertListeningHistoryAsync(item);
            }
        }
    }

    // ──────── Downloads ────────

    public Task<List<DownloadedAudio>> GetDownloadedAudiosAsync()
    {
        return GetDownloadedAudiosInternalAsync();
    }

    private async Task<List<DownloadedAudio>> GetDownloadedAudiosInternalAsync()
    {
        await EnsureLocalDataLoadedAsync();
        var removed = _downloads.Where(download => !File.Exists(download.LocalPath)).ToList();
        if (removed.Count > 0)
        {
            foreach (var item in removed)
            {
                _downloads.Remove(item);
                await _localDatabaseService.DeleteDownloadedAudioAsync(item.AudioGuideId);
            }
        }

        return _downloads.OrderByDescending(d => d.DownloadedAt).ToList();
    }

    public async Task<bool> DownloadAudioAsync(string audioGuideId)
    {
        await EnsureLocalDataLoadedAsync();
        if (_downloads.Any(d => d.AudioGuideId == audioGuideId))
            return false;

        var audioGuide = await GetAudioGuideByIdAsync(audioGuideId);
        if (audioGuide == null)
            return false;

        var sourceUrl = !string.IsNullOrWhiteSpace(audioGuide.CloudinaryAudioUrl)
            ? audioGuide.CloudinaryAudioUrl
            : audioGuide.AudioUrl;

        if (string.IsNullOrWhiteSpace(sourceUrl) || !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return false;

        Directory.CreateDirectory(DownloadDirectory);
        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp3";
        }

        var localPath = Path.Combine(DownloadDirectory, $"{audioGuideId}{extension}");

        using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            return false;

        await using (var networkStream = await response.Content.ReadAsStreamAsync())
        await using (var fileStream = File.Create(localPath))
        {
            await networkStream.CopyToAsync(fileStream);
        }

        var fileInfo = new FileInfo(localPath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
            return false;

        var download = new DownloadedAudio
        {
            AudioGuideId = audioGuideId,
            LocalPath = localPath,
            DownloadedAt = DateTime.Now,
            FileSize = fileInfo.Length
        };

        _downloads.Add(download);
        await _localDatabaseService.UpsertDownloadedAudioAsync(download);

        return true;
    }

    public async Task<bool> DeleteDownloadedAudioAsync(string audioGuideId)
    {
        await EnsureLocalDataLoadedAsync();
        var item = _downloads.FirstOrDefault(d => d.AudioGuideId == audioGuideId);
        if (item != null)
        {
            if (File.Exists(item.LocalPath))
            {
                File.Delete(item.LocalPath);
            }
            _downloads.Remove(item);
            await _localDatabaseService.DeleteDownloadedAudioAsync(audioGuideId);
            return true;
        }
        return false;
    }

    public async Task<long> GetTotalDownloadSizeAsync()
    {
        await EnsureLocalDataLoadedAsync();
        return _downloads.Sum(d => d.FileSize);
    }

    private async Task EnsureLocalDataLoadedAsync()
    {
        if (_localDataLoaded)
        {
            return;
        }

        await _localDataSyncLock.WaitAsync();
        try
        {
            if (_localDataLoaded)
            {
                return;
            }

            var savedFavoriteIds = await _localDatabaseService.GetFavoriteLocationIdsAsync();
            _favoriteLocationIds.Clear();
            if (savedFavoriteIds.Count == 0)
            {
                foreach (var id in CreateDefaultFavoriteLocationIds())
                {
                    _favoriteLocationIds.Add(id);
                }
                await _localDatabaseService.SaveFavoriteLocationIdsAsync(_favoriteLocationIds.ToList());
            }
            else
            {
                foreach (var id in savedFavoriteIds)
                {
                    _favoriteLocationIds.Add(id);
                }
            }

            var savedHistory = await _localDatabaseService.GetListeningHistoryAsync();
            _history.Clear();
            if (savedHistory.Count == 0)
            {
                var defaults = CreateDefaultHistory();
                foreach (var item in defaults)
                {
                    _history.Add(item);
                    await _localDatabaseService.UpsertListeningHistoryAsync(item);
                }
            }
            else
            {
                _history.AddRange(savedHistory);
            }

            var savedDownloads = await _localDatabaseService.GetDownloadedAudiosAsync();
            _downloads.Clear();
            _downloads.AddRange(savedDownloads);

            _localDataLoaded = true;
        }
        finally
        {
            _localDataSyncLock.Release();
        }
    }

    private static HashSet<string> CreateDefaultFavoriteLocationIds()
    {
        return new HashSet<string>(new[] { "loc_001", "loc_002", "loc_006" }, StringComparer.OrdinalIgnoreCase);
    }

    private static List<ListeningHistory> CreateDefaultHistory()
    {
        return SampleData.GetListeningHistory(LocalUserId);
    }

    // ──────── Helpers ────────

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
