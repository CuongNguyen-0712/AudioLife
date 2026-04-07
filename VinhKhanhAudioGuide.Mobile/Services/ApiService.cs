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

    private readonly List<Location> _locations;
    private readonly List<Category> _categories;
    private readonly List<Tour> _tours;
    private UserProfile _userProfile;
    private readonly List<ListeningHistory> _history;
    private readonly List<DownloadedAudio> _downloads;

    public ApiService()
    {
        _locations = SampleData.GetLocations();
        _categories = SampleData.GetCategories();
        _tours = SampleData.GetTours();
        _userProfile = new UserProfile
        {
            Id = "user_001",
            Name = "Nguyễn Văn A",
            Email = "nguyenvana@email.com",
            AvatarUrl = "default_avatar",
            PreferredLanguage = "vi",
            FavoriteLocationIds = new List<string> { "loc_001", "loc_002", "loc_006" },
            VisitedLocationIds = new List<string> { "loc_001", "loc_002", "loc_003", "loc_005", "loc_006" },
            CreatedAt = new DateTime(2025, 1, 15),
            LastLoginAt = DateTime.Now
        };

        _history = new List<ListeningHistory>
        {
            new()
            {
                Id = "h1",
                AudioGuideId = "ag_001_1",
                AudioTitle = "Giới thiệu quán",
                LocationId = "loc_001",
                LocationName = "Bún mắm Vĩnh Khánh",
                LocationImageUrl = "bun_mam.jpg",
                AudioDuration = 3,
                Progress = 0.8,
                ListenedAt = DateTime.Today.AddHours(-2)
            },
            new()
            {
                Id = "h2",
                AudioGuideId = "ag_002_1",
                AudioTitle = "Giới thiệu quán",
                LocationId = "loc_002",
                LocationName = "Bánh xèo miền Tây",
                LocationImageUrl = "banh_xeo.jpg",
                AudioDuration = 3,
                Progress = 1.0,
                ListenedAt = DateTime.Today.AddHours(-5)
            },
            new()
            {
                Id = "h3",
                AudioGuideId = "ag_007_1",
                AudioTitle = "Giới thiệu quán",
                LocationId = "loc_007",
                LocationName = "Ốc xào bơ tỏi",
                LocationImageUrl = "oc_xao_bo_toi.jpg",
                AudioDuration = 3,
                Progress = 0.45,
                ListenedAt = DateTime.Today.AddDays(-1)
            },
            new()
            {
                Id = "h4",
                AudioGuideId = "ag_006_1",
                AudioTitle = "Giới thiệu quán",
                LocationId = "loc_006",
                LocationName = "Phở khuya",
                LocationImageUrl = "pho.jpg",
                AudioDuration = 3,
                Progress = 1.0,
                ListenedAt = DateTime.Today.AddDays(-2)
            }
        };

        _downloads = new List<DownloadedAudio>();
    }

    // ──────── Locations ────────

    public Task<List<Location>> GetLocationsAsync()
        => Task.FromResult(_locations);

    public Task<Location?> GetLocationByIdAsync(string locationId)
        => Task.FromResult(_locations.FirstOrDefault(l => l.Id == locationId));

    public Task<List<Location>> SearchLocationsAsync(string query)
    {
        var q = query.ToLowerInvariant();
        var results = _locations.Where(l =>
            l.Name.Contains(q, StringComparison.InvariantCultureIgnoreCase) ||
            l.Description.Contains(q, StringComparison.InvariantCultureIgnoreCase) ||
            l.Address.Contains(q, StringComparison.InvariantCultureIgnoreCase)
        ).ToList();
        return Task.FromResult(results);
    }

    public Task<List<Location>> GetLocationsByCategoryAsync(string categoryId)
        => Task.FromResult(_locations.Where(l => l.CategoryId == categoryId).ToList());

    public Task<List<Location>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusKm = 0.1)
    {
        var nearby = _locations.Where(l =>
        {
            var distance = CalculateDistance(latitude, longitude, l.Latitude, l.Longitude);
            return distance <= radiusKm;
        }).OrderBy(l => CalculateDistance(latitude, longitude, l.Latitude, l.Longitude)).ToList();

        return Task.FromResult(nearby);
    }

    // ──────── Categories ────────

    public Task<List<Category>> GetCategoriesAsync()
        => Task.FromResult(_categories);

    // ──────── Tours ────────

    public Task<List<Tour>> GetToursAsync()
        => Task.FromResult(_tours);

    public Task<Tour?> GetTourByIdAsync(string tourId)
        => Task.FromResult(_tours.FirstOrDefault(t => t.Id == tourId));

    public Task<List<Tour>> GetFeaturedToursAsync()
        => Task.FromResult(_tours.Where(t => t.IsFeatured).ToList());

    // ──────── Audio ────────

    public Task<List<AudioGuide>> GetAudioGuidesForLocationAsync(string locationId)
    {
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        return Task.FromResult(location?.AudioGuides ?? new List<AudioGuide>());
    }

    public Task<AudioGuide?> GetAudioGuideByIdAsync(string audioGuideId)
    {
        var audio = _locations.SelectMany(l => l.AudioGuides)
                              .FirstOrDefault(a => a.Id == audioGuideId);
        return Task.FromResult(audio);
    }

    // ──────── User Profile ────────

    public Task<UserProfile?> GetUserProfileAsync()
        => Task.FromResult<UserProfile?>(_userProfile);

    public Task<bool> UpdateUserProfileAsync(UserProfile profile)
    {
        _userProfile = profile;
        return Task.FromResult(true);
    }

    public Task<bool> ToggleFavoriteAsync(string locationId)
    {
        if (_userProfile.FavoriteLocationIds.Contains(locationId))
            _userProfile.FavoriteLocationIds.Remove(locationId);
        else
            _userProfile.FavoriteLocationIds.Add(locationId);
        return Task.FromResult(true);
    }

    public Task<List<Location>> GetFavoriteLocationsAsync()
    {
        var favorites = _locations.Where(l => _userProfile.FavoriteLocationIds.Contains(l.Id)).ToList();
        return Task.FromResult(favorites);
    }

    // ──────── History ────────

    public Task<List<ListeningHistory>> GetListeningHistoryAsync()
        => Task.FromResult(_history.OrderByDescending(h => h.ListenedAt).ToList());

    public Task AddListeningHistoryAsync(string audioGuideId, string locationId, double progress)
    {
        var audio = _locations.SelectMany(l => l.AudioGuides).FirstOrDefault(a => a.Id == audioGuideId);
        var location = _locations.FirstOrDefault(l => l.Id == locationId);
        if (audio != null && location != null)
        {
            var existing = _history.FirstOrDefault(h => h.AudioGuideId == audioGuideId);
            if (existing != null)
            {
                existing.Progress = progress;
                existing.ListenedAt = DateTime.Now;
            }
            else
            {
                _history.Add(new ListeningHistory
                {
                    Id = $"h{_history.Count + 1}",
                    AudioGuideId = audioGuideId,
                    AudioTitle = audio.Title,
                    LocationId = locationId,
                    LocationName = location.Name,
                    LocationImageUrl = location.ImageUrl,
                    AudioDuration = audio.Duration,
                    Progress = progress,
                    ListenedAt = DateTime.Now
                });
            }
        }
        return Task.CompletedTask;
    }

    // ──────── Downloads ────────

    public Task<List<DownloadedAudio>> GetDownloadedAudiosAsync()
    {
        _downloads.RemoveAll(download => !File.Exists(download.LocalPath));
        return Task.FromResult(_downloads.OrderByDescending(d => d.DownloadedAt).ToList());
    }

    public async Task<bool> DownloadAudioAsync(string audioGuideId)
    {
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

        _downloads.Add(new DownloadedAudio
        {
            AudioGuideId = audioGuideId,
            LocalPath = localPath,
            DownloadedAt = DateTime.Now,
            FileSize = fileInfo.Length
        });

        return true;
    }

    public Task<bool> DeleteDownloadedAudioAsync(string audioGuideId)
    {
        var item = _downloads.FirstOrDefault(d => d.AudioGuideId == audioGuideId);
        if (item != null)
        {
            if (File.Exists(item.LocalPath))
            {
                File.Delete(item.LocalPath);
            }
            _downloads.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<long> GetTotalDownloadSizeAsync()
        => Task.FromResult(_downloads.Sum(d => d.FileSize));

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
