using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Tours;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Tour> Tours { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tours = await _db.Tours
            .Include(t => t.TourLocations)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}
