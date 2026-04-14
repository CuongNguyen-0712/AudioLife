using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuthOptions _authOptions;
    private readonly IPoiChangeRequestService _changeRequestService;

    public IndexModel(AppDbContext db, IOptions<AuthOptions> authOptions, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _authOptions = authOptions.Value;
        _changeRequestService = changeRequestService;
    }

    public int LocationCount { get; set; }
    public int CategoryCount { get; set; }
    public int TourCount { get; set; }
    public int AudioGuideCount { get; set; }
    public int PoiAdminCount { get; set; }
    public int PendingRequestCount { get; set; }
    public int InReviewRequestCount { get; set; }
    public int UnassignedLocationCount { get; set; }
    public int LocationsWithAudioCount { get; set; }
    public double AverageAudioPerLocation { get; set; }
    public List<LocationAudioSummary> TopLocationsByAudio { get; set; } = new();
    public List<CategoryAudioSummary> CategorySummaries { get; set; } = new();
    public List<PoiAdminSummary> PoiAdminSummaries { get; set; } = new();
    public List<RecentRequestSummary> RecentRequests { get; set; } = new();

    public async Task OnGetAsync()
    {
        LocationCount = await _db.Locations.CountAsync();
        CategoryCount = await _db.Categories.CountAsync();
        TourCount = await _db.Tours.CountAsync();
        AudioGuideCount = await _db.AudioGuides.CountAsync();

        PoiAdminSummaries = await BuildPoiAdminSummariesAsync();
        PoiAdminCount = PoiAdminSummaries.Count;

        var requests = await _changeRequestService.GetAllAsync();
        PendingRequestCount = requests.Count(item => item.Status == PoiChangeRequestStatus.Pending);
        InReviewRequestCount = requests.Count(item => item.Status == PoiChangeRequestStatus.InReview);
        RecentRequests = requests
            .Take(8)
            .Select(item => new RecentRequestSummary
            {
                Id = item.Id,
                SubmittedAtUtc = item.SubmittedAtUtc,
                SubmittedByName = item.SubmittedByName,
                LocationName = item.LocationName,
                Title = item.Title,
                Status = item.Status,
                ReviewNote = item.ReviewNote
            })
            .ToList();

        var assignedLocationIds = PoiAdminSummaries
            .SelectMany(summary => summary.LocationIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        UnassignedLocationCount = await _db.Locations
            .AsNoTracking()
            .CountAsync(location => !assignedLocationIds.Contains(location.Id));

        TopLocationsByAudio = await _db.Locations
            .AsNoTracking()
            .Select(location => new LocationAudioSummary
            {
                LocationId = location.Id,
                LocationName = location.Name,
                CategoryName = location.Category != null ? location.Category.Name : "Chưa phân loại",
                AudioCount = location.AudioGuides.Count,
                Duration = location.Duration
            })
            .OrderByDescending(item => item.AudioCount)
            .ThenBy(item => item.LocationName)
            .Take(6)
            .ToListAsync();

        CategorySummaries = await _db.Categories
            .AsNoTracking()
            .Select(category => new CategoryAudioSummary
            {
                CategoryName = category.Name,
                LocationCount = category.Locations.Count,
                AudioCount = category.Locations.SelectMany(location => location.AudioGuides).Count()
            })
            .OrderByDescending(item => item.AudioCount)
            .ThenBy(item => item.CategoryName)
            .ToListAsync();

        LocationsWithAudioCount = await _db.Locations
            .AsNoTracking()
            .CountAsync(location => location.AudioGuides.Any());

        AverageAudioPerLocation = LocationCount == 0
            ? 0
            : Math.Round((double)AudioGuideCount / LocationCount, 1);
    }

    private async Task<List<PoiAdminSummary>> BuildPoiAdminSummariesAsync()
    {
        var assignmentMap = await _db.PoiAdminLocationAssignments
            .AsNoTracking()
            .GroupBy(item => item.Username)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(item => item.LocationId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var poiAdmins = _authOptions.Users
            .Where(user => string.Equals(user.Role, RoleNames.PoiAdmin, StringComparison.OrdinalIgnoreCase))
            .Select(user => new PoiAdminSummary
            {
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName,
                Username = user.Username,
                LocationIds = (assignmentMap.TryGetValue(user.Username, out var assignedIds) ? assignedIds : user.LocationIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .OrderBy(user => user.DisplayName)
            .ToList();

        if (!poiAdmins.Any())
        {
            return poiAdmins;
        }

        var locationLookup = await _db.Locations
            .AsNoTracking()
            .Select(location => new { location.Id, location.Name })
            .ToDictionaryAsync(location => location.Id, location => location.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var admin in poiAdmins)
        {
            admin.LocationNames = admin.LocationIds
                .Select(id => locationLookup.TryGetValue(id, out var name) ? name : $"{id} (không tồn tại)")
                .ToList();
        }

        return poiAdmins;
    }

    public class LocationAudioSummary
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int AudioCount { get; set; }
        public int Duration { get; set; }
    }

    public class CategoryAudioSummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public int LocationCount { get; set; }
        public int AudioCount { get; set; }
    }

    public class PoiAdminSummary
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> LocationIds { get; set; } = new();
        public List<string> LocationNames { get; set; } = new();
    }

    public class RecentRequestSummary
    {
        public Guid Id { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public string SubmittedByName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public PoiChangeRequestStatus Status { get; set; }
        public string? ReviewNote { get; set; }
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
