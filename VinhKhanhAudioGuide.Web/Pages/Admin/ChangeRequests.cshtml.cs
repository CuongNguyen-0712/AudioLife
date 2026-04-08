using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class ChangeRequestsModel : PageModel
{
    private readonly IPoiChangeRequestService _changeRequestService;

    public ChangeRequestsModel(IPoiChangeRequestService changeRequestService)
    {
        _changeRequestService = changeRequestService;
    }

    public List<PoiChangeRequest> Requests { get; set; } = new();
    public int PendingCount { get; set; }
    public int InReviewCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid id, PoiChangeRequestStatus status)
    {
        var updatedBy = User.Identity?.Name ?? "SystemAdmin";
        var success = await _changeRequestService.TryUpdateStatusAsync(id, status, updatedBy);
        TempData["Success"] = success
            ? "Đã cập nhật trạng thái yêu cầu."
            : "Không thể cập nhật yêu cầu. Vui lòng kiểm tra dữ liệu request.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Requests = (await _changeRequestService.GetAllAsync()).ToList();
        PendingCount = Requests.Count(item => item.Status == PoiChangeRequestStatus.Pending);
        InReviewCount = Requests.Count(item => item.Status == PoiChangeRequestStatus.InReview);
        ApprovedCount = Requests.Count(item => item.Status == PoiChangeRequestStatus.Approved);
        RejectedCount = Requests.Count(item => item.Status == PoiChangeRequestStatus.Rejected);
    }
}
