using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class TranslationsModel : PageModel
{
    private static readonly string[] SupportedLanguages = ["vi", "en", "fr", "ja", "ko", "zh"];

    private readonly AppDbContext _db;

    public TranslationsModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Language { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyIssues { get; set; }

    public List<LocationFilterItem> Locations { get; set; } = new();
    public List<LocationCoverageItem> CoverageByLocation { get; set; } = new();
    public List<AudioTranslationItem> TranslationItems { get; set; } = new();

    public int TotalAudioCount { get; set; }
    public int MissingAudioCount { get; set; }
    public int MissingTranscriptCount { get; set; }
    public int ReadyCount { get; set; }

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new LocationFilterItem
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToListAsync();

        var coverageSource = await _db.AudioGuides
            .AsNoTracking()
            .Include(item => item.Location)
            .Where(item => string.IsNullOrWhiteSpace(LocationId) || item.LocationId == LocationId)
            .ToListAsync();

        CoverageByLocation = coverageSource
            .GroupBy(item => new { item.LocationId, LocationName = item.Location != null ? item.Location.Name : item.LocationId })
            .Select(group =>
            {
                var normalized = group
                    .Select(item => NormalizeLanguage(item.Language))
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = SupportedLanguages
                    .Where(lang => !normalized.Contains(lang))
                    .ToList();

                return new LocationCoverageItem
                {
                    LocationId = group.Key.LocationId,
                    LocationName = group.Key.LocationName,
                    TotalAudios = group.Count(),
                    ViCount = group.Count(item => NormalizeLanguage(item.Language) == "vi"),
                    EnCount = group.Count(item => NormalizeLanguage(item.Language) == "en"),
                    FrCount = group.Count(item => NormalizeLanguage(item.Language) == "fr"),
                    JaCount = group.Count(item => NormalizeLanguage(item.Language) == "ja"),
                    KoCount = group.Count(item => NormalizeLanguage(item.Language) == "ko"),
                    ZhCount = group.Count(item => NormalizeLanguage(item.Language) == "zh"),
                    MissingLanguages = missing
                };
            })
            .OrderBy(item => item.LocationName)
            .ToList();

        var query = _db.AudioGuides
            .AsNoTracking()
            .Include(item => item.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(LocationId))
        {
            query = query.Where(item => item.LocationId == LocationId);
        }

        if (!string.IsNullOrWhiteSpace(Language))
        {
            var normalizedLanguage = NormalizeLanguage(Language);
            query = query.Where(item => item.Language == normalizedLanguage);
        }

        TranslationItems = await query
            .OrderBy(item => item.Location != null ? item.Location.Name : item.LocationId)
            .ThenBy(item => item.Title)
            .ThenBy(item => item.Language)
            .Select(item => new AudioTranslationItem
            {
                Id = item.Id,
                Title = item.Title,
                LocationId = item.LocationId,
                LocationName = item.Location != null ? item.Location.Name : item.LocationId,
                Language = item.Language,
                HasAudio = !string.IsNullOrWhiteSpace(item.AudioUrl) || !string.IsNullOrWhiteSpace(item.CloudinaryAudioUrl),
                HasTranscript = !string.IsNullOrWhiteSpace(item.TranscriptText),
                Description = item.Description
            })
            .ToListAsync();

        foreach (var item in TranslationItems)
        {
            item.NormalizedLanguage = NormalizeLanguage(item.Language);
            item.IsSupportedLanguage = SupportedLanguages.Contains(item.NormalizedLanguage, StringComparer.OrdinalIgnoreCase);
            item.IsIssue = !item.HasAudio || !item.HasTranscript || !item.IsSupportedLanguage;
        }

        if (OnlyIssues)
        {
            TranslationItems = TranslationItems.Where(item => item.IsIssue).ToList();
        }

        TotalAudioCount = TranslationItems.Count;
        MissingAudioCount = TranslationItems.Count(item => !item.HasAudio);
        MissingTranscriptCount = TranslationItems.Count(item => !item.HasTranscript);
        ReadyCount = TranslationItems.Count(item => !item.IsIssue);
    }

    private static string NormalizeLanguage(string? language)
    {
        return (language ?? string.Empty).Trim().ToLowerInvariant();
    }

    public sealed class LocationFilterItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public sealed class LocationCoverageItem
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int TotalAudios { get; set; }
        public int ViCount { get; set; }
        public int EnCount { get; set; }
        public int FrCount { get; set; }
        public int JaCount { get; set; }
        public int KoCount { get; set; }
        public int ZhCount { get; set; }
        public List<string> MissingLanguages { get; set; } = new();
    }

    public sealed class AudioTranslationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string NormalizedLanguage { get; set; } = string.Empty;
        public bool IsSupportedLanguage { get; set; }
        public bool HasAudio { get; set; }
        public bool HasTranscript { get; set; }
        public bool IsIssue { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
