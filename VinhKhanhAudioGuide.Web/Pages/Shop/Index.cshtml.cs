using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public IndexModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    public List<Location> Locations { get; set; } = new();
    public Location? SelectedLocation { get; set; }
    public int AudioGuideCount { get; set; }
    public int AudioDurationMinutes { get; set; }
    public int TourCount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<AudioGuide> RecentAudioGuides { get; set; } = new();
    public List<Tour> RelatedTours { get; set; } = new();
    public List<PoiChangeRequest> RecentSubmittedRequests { get; set; } = new();
    public int PendingRequestCount { get; set; }
    public int InReviewRequestCount { get; set; }
    public int ApprovedRequestCount { get; set; }
    public int RejectedRequestCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedLocationId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);
        IQueryable<Location> query = _db.Locations
            .AsNoTracking()
            .Where(location => ownedLocationIds.Contains(location.Id));

        Locations = await query.OrderBy(location => location.Name).ToListAsync();

        if (!Locations.Any())
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(SelectedLocationId))
        {
            SelectedLocationId = Locations[0].Id;
        }

        if (!await UserAccessService.CanAccessLocationAsync(User, _db, SelectedLocationId))
        {
            return Forbid();
        }

        SelectedLocation = await _db.Locations
            .AsNoTracking()
            .Include(location => location.Category)
            .FirstOrDefaultAsync(l => l.Id == SelectedLocationId);

        if (SelectedLocation is null)
        {
            return Page();
        }

        AudioGuideCount = await _db.AudioGuides
            .AsNoTracking()
            .CountAsync(ag => ag.LocationId == SelectedLocation.Id);

        AudioDurationMinutes = await _db.AudioGuides
            .AsNoTracking()
            .Where(ag => ag.LocationId == SelectedLocation.Id)
            .Select(ag => ag.Duration)
            .SumAsync();

        RecentAudioGuides = await _db.AudioGuides
            .AsNoTracking()
            .Where(ag => ag.LocationId == SelectedLocation.Id)
            .OrderByDescending(ag => ag.Id)
            .Take(5)
            .ToListAsync();

        var relatedToursRaw = await _db.TourLocations
            .AsNoTracking()
            .Where(tl => tl.LocationId == SelectedLocation.Id)
            .OrderBy(tl => tl.SortOrder)
            .Select(tl => tl.Tour)
            .ToListAsync();

        RelatedTours = relatedToursRaw
            .Where(tour => tour != null)
            .Select(tour => tour!)
            .GroupBy(tour => tour.Id)
            .Select(group => group.First())
            .ToList();

        TourCount = RelatedTours.Count;
        CategoryName = SelectedLocation.Category?.Name ?? "Chưa phân loại";

        var submitterAliases = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name
        };

        var submittedRequests = await _changeRequestService.GetBySubmitterAliasesAsync(submitterAliases);
        RecentSubmittedRequests = submittedRequests
            .Take(8)
            .ToList();

        PendingRequestCount = submittedRequests.Count(item => item.Status == PoiChangeRequestStatus.Pending);
        InReviewRequestCount = submittedRequests.Count(item => item.Status == PoiChangeRequestStatus.InReview);
        ApprovedRequestCount = submittedRequests.Count(item => item.Status == PoiChangeRequestStatus.Approved);
        RejectedRequestCount = submittedRequests.Count(item => item.Status == PoiChangeRequestStatus.Rejected);

        return Page();
    }

    public static string GetStatusBadgeClass(PoiChangeRequestStatus status)
    {
        return status switch
        {
            PoiChangeRequestStatus.Pending => "text-bg-secondary",
            PoiChangeRequestStatus.InReview => "text-bg-warning",
            PoiChangeRequestStatus.Approved => "text-bg-success",
            PoiChangeRequestStatus.Rejected => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }
}
