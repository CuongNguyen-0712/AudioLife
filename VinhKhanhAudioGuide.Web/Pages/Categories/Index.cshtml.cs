using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public List<Category> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories
            .Include(c => c.Locations)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
