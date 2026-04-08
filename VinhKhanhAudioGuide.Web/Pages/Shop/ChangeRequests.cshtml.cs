using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Shop;

public class ChangeRequestsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPoiChangeRequestService _changeRequestService;

    public ChangeRequestsModel(AppDbContext db, IPoiChangeRequestService changeRequestService)
    {
        _db = db;
        _changeRequestService = changeRequestService;
    }

    public List<Location> AccessibleLocations { get; set; } = new();
    public List<PoiChangeRequest> SubmittedRequests { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var ownedLocationIds = await UserAccessService.GetOwnedLocationIdsAsync(User, _db);
        AccessibleLocations = await _db.Locations
            .AsNoTracking()
            .Where(location => ownedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToListAsync();

        var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.Identity?.Name
                       ?? string.Empty;

        SubmittedRequests = (await _changeRequestService
            .GetBySubmitterAsync(username))
            .Take(10)
            .ToList();
    }
}
