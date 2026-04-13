using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class AnalyticsModel : PageModel
{
    private readonly AppDbContext _db;

    public AnalyticsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public string SelectedLocationName { get; set; } = string.Empty;
    public int TotalAudios { get; set; }
    public int TotalAudioMinutes { get; set; }
    public int TotalHistoryRecords { get; set; }
    public int TotalCompletedRecords { get; set; }
    public int TotalListenSeconds { get; set; }
    public double AverageProgressPercent { get; set; }
    public List<AudioRankItem> TopAudios { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);
        IQueryable<Location> query = _db.Locations
            .AsNoTracking()
            .Where(location => ownedLocationIds.Contains(location.Id));

        AccessibleLocations = await query.OrderBy(location => location.Name).ToListAsync();
        if (!AccessibleLocations.Any()) return Page();

        if (string.IsNullOrWhiteSpace(LocationId))
        {
            LocationId = AccessibleLocations[0].Id;
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, LocationId)) return Forbid();

        var selectedLocation = AccessibleLocations.FirstOrDefault(location => location.Id == LocationId);
        SelectedLocationName = selectedLocation?.Name ?? string.Empty;

        var audios = await _db.AudioGuides
            .AsNoTracking()
            .Where(audio => audio.LocationId == LocationId)
            .OrderBy(audio => audio.Title)
            .ToListAsync();

        TotalAudios = audios.Count;
        TotalAudioMinutes = audios.Sum(audio => audio.Duration);

        var histories = await _db.ListeningHistories
            .AsNoTracking()
            .Where(item => item.LocationId == LocationId)
            .ToListAsync();

        TotalHistoryRecords = histories.Count;
        TotalCompletedRecords = histories.Count(item => item.IsCompleted);
        TotalListenSeconds = histories.Sum(item => item.ListenedSeconds);
        AverageProgressPercent = histories.Count == 0
            ? 0
            : Math.Round(histories.Average(item => (double)item.Progress) * 100, 1);

        var historyByAudio = histories
            .GroupBy(item => item.AudioGuideId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    ListenSeconds = group.Sum(item => item.ListenedSeconds),
                    ListenCount = group.Count(),
                    CompletionRate = group.Average(item => (double)item.Progress) * 100
                },
                StringComparer.OrdinalIgnoreCase);

        TopAudios = audios
            .Select(audio =>
            {
                historyByAudio.TryGetValue(audio.Id, out var usage);

                return new AudioRankItem
                {
                    Title = audio.Title,
                    Duration = audio.Duration,
                    ListenSeconds = usage?.ListenSeconds ?? 0,
                    ListenCount = usage?.ListenCount ?? 0,
                    CompletionRatePercent = Math.Round(usage?.CompletionRate ?? 0, 1)
                };
            })
            .OrderByDescending(item => item.ListenSeconds)
            .ThenByDescending(item => item.ListenCount)
            .ThenBy(item => item.Title)
            .Take(5)
            .ToList();

        if (TopAudios.All(item => item.ListenSeconds == 0))
        {
            TopAudios = audios
                .Select(audio => new AudioRankItem
                {
                    Title = audio.Title,
                    Duration = audio.Duration,
                    ListenSeconds = 0,
                    ListenCount = 0,
                    CompletionRatePercent = 0
                })
                .OrderBy(item => item.Title)
                .Take(5)
                .ToList();
        }

        return Page();
    }

    public class AudioRankItem
    {
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int ListenSeconds { get; set; }
        public int ListenCount { get; set; }
        public double CompletionRatePercent { get; set; }
    }
}
