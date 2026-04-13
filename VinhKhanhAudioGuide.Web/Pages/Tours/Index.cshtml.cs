using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanhAudioGuide.Web.Pages.Tours;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Tour> Tours { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool FeaturedOnly { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.Tours
            .Include(t => t.TourLocations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var normalized = Keyword.Trim();
            query = query.Where(item =>
                EF.Functions.Like(item.Name, $"%{normalized}%") ||
                EF.Functions.Like(item.Description, $"%{normalized}%"));
        }

        if (FeaturedOnly)
        {
            query = query.Where(item => item.IsFeatured);
        }

        Tours = await query
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}
