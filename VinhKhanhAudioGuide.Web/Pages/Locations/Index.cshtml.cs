using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Locations;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Location> Locations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .Include(l => l.Category)
            .Include(l => l.AudioGuides)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }
}
