using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Locations;

public class MapViewModel : PageModel
{
    private readonly AppDbContext _db;
    public MapViewModel(AppDbContext db) { _db = db; }

    public List<Location> Locations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations
            .Include(l => l.Category)
            .Include(l => l.AudioGuides)
            .Where(l => l.Latitude != 0 && l.Longitude != 0)
            .ToListAsync();
    }
}