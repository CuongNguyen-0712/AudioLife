using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanhAudioGuide.Web.Pages.Locations;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Location> Locations { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyNoAudio { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        var query = _db.Locations
            .AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.AudioGuides)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var normalized = Keyword.Trim();
            query = query.Where(item =>
                EF.Functions.Like(item.Name, $"%{normalized}%") ||
                EF.Functions.Like(item.Address, $"%{normalized}%") ||
                EF.Functions.Like(item.Description, $"%{normalized}%"));
        }

        if (!string.IsNullOrWhiteSpace(CategoryId))
        {
            query = query.Where(item => item.CategoryId == CategoryId);
        }

        if (OnlyNoAudio)
        {
            query = query.Where(item => !item.AudioGuides.Any());
        }

        Locations = await query
            .OrderBy(l => l.Name)
            .ToListAsync();
    }
}
