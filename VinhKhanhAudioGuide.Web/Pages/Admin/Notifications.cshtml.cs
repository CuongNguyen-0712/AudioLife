using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

[Authorize(Policy = "SystemAdminOnly")]
public class NotificationsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public NotificationsModel(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [BindProperty]
    public NotificationInput Input { get; set; } = new();

    public List<AppUser> TargetUsers { get; set; } = new();
    public int ActiveDeviceCount { get; set; }

    public class NotificationInput
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Body { get; set; } = string.Empty;

        public Guid? TargetUserId { get; set; }
        public bool SendToAll { get; set; }
    }

    public async Task OnGetAsync()
    {
        TargetUsers = await _db.AppUsers
            .Where(u => _db.UserDeviceTokens.Any(t => t.UserId == u.Id && t.IsActive))
            .OrderBy(u => u.QrCodeValue)
            .ToListAsync();

        ActiveDeviceCount = await _db.UserDeviceTokens.CountAsync(t => t.IsActive);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        bool overallSuccess = true;

        if (Input.SendToAll)
        {
            var userIdsWithDevices = await _db.UserDeviceTokens
                .Where(t => t.IsActive)
                .Select(t => t.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIdsWithDevices)
            {
                var success = await _notificationService.SendPushNotificationAsync(userId, Input.Title, Input.Body);
                if (!success) overallSuccess = false;
            }

            TempData["Message"] = overallSuccess 
                ? $"Đã gửi thông báo tới {userIdsWithDevices.Count} người dùng." 
                : "Có lỗi xảy ra khi gửi thông báo tới một số thiết bị.";
        }
        else if (Input.TargetUserId.HasValue)
        {
            var success = await _notificationService.SendPushNotificationAsync(Input.TargetUserId.Value, Input.Title, Input.Body);
            TempData["Message"] = success ? "Đã gửi thông báo thành công." : "Gửi thông báo thất bại.";
        }

        return RedirectToPage();
    }
}
