using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.AudioGuides;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<AudioGuide> AudioGuides { get; set; } = new();

    public async Task OnGetAsync()
    {
        AudioGuides = await _db.AudioGuides
            .Include(ag => ag.Location)
            .OrderBy(ag => ag.LocationId)
            .ThenBy(ag => ag.Title)
            .ToListAsync();
    }
}
