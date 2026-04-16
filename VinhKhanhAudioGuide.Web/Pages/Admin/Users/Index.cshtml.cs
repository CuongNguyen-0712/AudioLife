using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class EndUsersModel : PageModel
{
    private readonly AppDbContext _context;

    public EndUsersModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<AppUser> Users { get; set; } = new List<AppUser>();
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public int TotalUserCount { get; set; }
    public int ActiveUserCount { get; set; }
    public int BlockedUserCount { get; set; }
    public int UsersWithActiveSubscriptionCount { get; set; }
    public int ActiveSessionCount { get; set; }

    public async Task OnGetAsync(string searchTerm = "", string statusFilter = "")
    {
        SearchTerm = searchTerm;
        StatusFilter = statusFilter;

        var query = _context.AppUsers
            .Include(u => u.Subscriptions)
            .ThenInclude(s => s.Package)
            .Include(u => u.AppSessions)
            .Include(u => u.ListeningHistories)
            .AsSplitQuery()
            .AsQueryable();

        // Search by email, phone, display name, or QR code
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                EF.Functions.Like(u.QrCodeValue, $"%{searchTerm.Trim()}%") ||
                EF.Functions.Like(u.DisplayName ?? string.Empty, $"%{searchTerm.Trim()}%") ||
                EF.Functions.Like(u.PhoneNumber ?? string.Empty, $"%{searchTerm.Trim()}%") ||
                EF.Functions.Like(u.Email ?? string.Empty, $"%{searchTerm.Trim()}%"));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(u => u.Status == statusFilter);
        }

        Users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .ToListAsync();

        TotalUserCount = await _context.AppUsers.CountAsync();
        ActiveUserCount = await _context.AppUsers.CountAsync(u => u.Status == "Active");
        BlockedUserCount = await _context.AppUsers.CountAsync(u => u.Status == "Blocked");
        UsersWithActiveSubscriptionCount = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.Status == "Active")
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();
        ActiveSessionCount = await _context.UserAppSessions
            .AsNoTracking()
            .Where(session => session.IsActive && session.RevokedAtUtc == null && session.ExpiresAtUtc > DateTime.UtcNow)
            .CountAsync();
    }

    public async Task<IActionResult> OnPostBlockAsync(Guid id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Status = "Blocked";
        _context.Update(user);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnblockAsync(Guid id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.Status = "Active";
        _context.Update(user);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var user = await _context.AppUsers
            .Include(u => u.AppSessions)
            .Include(u => u.Subscriptions)
            .Include(u => u.ListeningHistories)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        // Delete related data
        _context.UserAppSessions.RemoveRange(user.AppSessions);
        _context.UserSubscriptions.RemoveRange(user.Subscriptions);
        _context.ListeningHistories.RemoveRange(user.ListeningHistories);
        _context.AppUsers.Remove(user);

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

}
