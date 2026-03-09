using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;

namespace VinhKhanhAudioGuide.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) { _db = db; }

    public int LocationCount { get; set; }
    public int CategoryCount { get; set; }
    public int TourCount { get; set; }
    public int AudioGuideCount { get; set; }

    public async Task OnGetAsync()
    {
        LocationCount = await _db.Locations.CountAsync();
        CategoryCount = await _db.Categories.CountAsync();
        TourCount = await _db.Tours.CountAsync();
        AudioGuideCount = await _db.AudioGuides.CountAsync();
    }
}
