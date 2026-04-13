using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<AudioGuide> AudioGuides { get; set; } = new();
    public List<Location> Locations { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Language { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool TtsOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyIssues { get; set; }

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        var query = _db.AudioGuides
            .AsNoTracking()
            .Include(ag => ag.Location)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var normalized = Keyword.Trim();
            query = query.Where(item =>
                EF.Functions.Like(item.Title, $"%{normalized}%") ||
                EF.Functions.Like(item.Description, $"%{normalized}%") ||
                EF.Functions.Like(item.TranscriptText, $"%{normalized}%"));
        }

        if (!string.IsNullOrWhiteSpace(LocationId))
        {
            query = query.Where(item => item.LocationId == LocationId);
        }

        if (!string.IsNullOrWhiteSpace(Language))
        {
            var normalizedLanguage = Language.Trim().ToLowerInvariant();
            query = query.Where(item => item.Language == normalizedLanguage);
        }

        if (TtsOnly)
        {
            query = query.Where(item => item.GeneratedFromTts);
        }

        if (OnlyIssues)
        {
            query = query.Where(item =>
                string.IsNullOrWhiteSpace(item.TranscriptText) ||
                (string.IsNullOrWhiteSpace(item.AudioUrl) && string.IsNullOrWhiteSpace(item.CloudinaryAudioUrl)));
        }

        AudioGuides = await query
            .OrderBy(ag => ag.LocationId)
            .ThenBy(ag => ag.Title)
            .ToListAsync();
    }
}
