using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using Microsoft.AspNetCore.Authorization;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

[Authorize(Roles = RoleNames.SystemAdmin)]
public class ReviewsModel : PageModel
{
    private readonly AppDbContext _db;

    public ReviewsModel(AppDbContext db)
    {
        _db = db;
    }

    public List<LocationReview> Reviews { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? LocationFilter { get; set; }

    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    
    public List<Location> Locations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Locations = await _db.Locations.OrderBy(l => l.Name).ToListAsync();

        var query = _db.LocationReviews
            .Include(r => r.Location)
            .AsQueryable();

        PendingCount = await _db.LocationReviews.CountAsync(r => r.Status == ReviewStatus.Pending);
        ApprovedCount = await _db.LocationReviews.CountAsync(r => r.Status == ReviewStatus.Approved);
        RejectedCount = await _db.LocationReviews.CountAsync(r => r.Status == ReviewStatus.Rejected);

        if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<ReviewStatus>(StatusFilter, true, out var parsedStatusFilter))
        {
            query = query.Where(r => r.Status == parsedStatusFilter);
        }

        if (!string.IsNullOrEmpty(LocationFilter))
        {
            query = query.Where(r => r.LocationId == LocationFilter);
        }

        Reviews = await query.OrderByDescending(r => r.CreatedAtUtc).ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid id, string status)
    {
        var review = await _db.LocationReviews.FindAsync(id);
        if (review == null) return NotFound();
        if (!Enum.TryParse<ReviewStatus>(status, true, out var parsedStatus)) return BadRequest();

        review.Status = parsedStatus;
        review.ReviewedBy = User.Identity?.Name ?? "System";
        review.ReviewedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái đánh giá thành {status}.";
        return RedirectToPage(new { StatusFilter, LocationFilter });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var review = await _db.LocationReviews.FindAsync(id);
        if (review == null) return NotFound();

        _db.LocationReviews.Remove(review);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa đánh giá.";
        return RedirectToPage(new { StatusFilter, LocationFilter });
    }
}
