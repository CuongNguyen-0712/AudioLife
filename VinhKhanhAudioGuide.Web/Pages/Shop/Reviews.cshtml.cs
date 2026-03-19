using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class ReviewsModel : PageModel
{
    private readonly AppDbContext _db;

    public ReviewsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public List<ReviewItem> Reviews { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        IQueryable<Location> query = _db.Locations.AsNoTracking();
        if (!UserAccessService.IsAdmin(User))
        {
            var ownedLocationIds = UserAccessService.GetOwnedLocationIds(User);
            query = query.Where(location => ownedLocationIds.Contains(location.Id));
        }

        AccessibleLocations = await query.OrderBy(location => location.Name).ToListAsync();
        if (!AccessibleLocations.Any()) return Page();

        if (string.IsNullOrWhiteSpace(LocationId))
        {
            LocationId = AccessibleLocations[0].Id;
        }

        var selectedLocationId = LocationId;
        if (string.IsNullOrWhiteSpace(selectedLocationId))
        {
            return Page();
        }

        if (!UserAccessService.CanAccessLocation(User, selectedLocationId)) return Forbid();

        var audios = await _db.AudioGuides
            .AsNoTracking()
            .Where(audio => audio.LocationId == selectedLocationId)
            .OrderBy(audio => audio.Title)
            .ToListAsync();

        Reviews = audios.Select((audio, index) => new ReviewItem
        {
            UserName = $"Khách {index + 1}",
            AudioTitle = audio.Title,
            Rating = 3 + (index % 3),
            Comment = $"Nội dung {(audio.Title ?? "audio").ToLowerInvariant()} dễ nghe và hợp gu ẩm thực đêm Vĩnh Khánh.",
            PendingReply = index % 2 == 0
        }).ToList();

        return Page();
    }

    public class ReviewItem
    {
        public string UserName { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool PendingReply { get; set; }
    }
}
