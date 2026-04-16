using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class PaymentPackagesModel : PageModel
{
    private readonly AppDbContext _context;

    public PaymentPackagesModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<PaymentPackage> Packages { get; set; } = new List<PaymentPackage>();
    public int ActiveSubscriptionCount { get; set; }
    public int PendingSubscriptionCount { get; set; }
    public int InactivePackageCount { get; set; }
    public int ActivePackageCount { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;

    public async Task OnGetAsync(string searchTerm = "", string statusFilter = "")
    {
        SearchTerm = searchTerm;
        StatusFilter = statusFilter;

        var query = _context.PaymentPackages
            .AsNoTracking()
            .Include(p => p.Subscriptions)
            .ThenInclude(s => s.User)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(package =>
                EF.Functions.Like(package.Id, $"%{term}%") ||
                EF.Functions.Like(package.Name, $"%{term}%") ||
                EF.Functions.Like(package.Description ?? string.Empty, $"%{term}%") ||
                EF.Functions.Like(package.Currency, $"%{term}%"));
        }

        if (string.Equals(statusFilter, "Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(package => package.IsActive);
        }
        else if (string.Equals(statusFilter, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(package => !package.IsActive);
        }

        Packages = await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Price)
            .ToListAsync();

        ActivePackageCount = await _context.PaymentPackages.CountAsync(package => package.IsActive);
        InactivePackageCount = await _context.PaymentPackages.CountAsync(package => !package.IsActive);
        ActiveSubscriptionCount = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.Status == "Active")
            .CountAsync();
        PendingSubscriptionCount = await _context.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.Status == "Pending")
            .CountAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        var package = await _context.PaymentPackages.FindAsync(id);
        if (package == null)
        {
            return NotFound();
        }

        package.IsActive = !package.IsActive;
        _context.Update(package);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var package = await _context.PaymentPackages
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package == null)
        {
            return NotFound();
        }

        // Can only delete if no active subscriptions
        if (package.Subscriptions.Any(s => s.Status is "Active" or "Pending"))
        {
            TempData["ErrorMessage"] = "Không thể xóa gói này vì còn có gói đăng ký hoạt động.";
            return RedirectToPage();
        }

        _context.PaymentPackages.Remove(package);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gói thanh toán đã được xóa thành công.";
        return RedirectToPage();
    }
}

public class PaymentPackageEditModel : PageModel
{
    private readonly AppDbContext _context;

    public PaymentPackageEditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public PaymentPackage Package { get; set; } = new();

    public bool IsCreate { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        IsCreate = string.IsNullOrEmpty(id);

        if (!IsCreate)
        {
            var package = await _context.PaymentPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }

            Package = package;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        IsCreate = string.IsNullOrEmpty(Package.Id);

        if (IsCreate)
        {
            Package.CreatedAtUtc = DateTime.UtcNow;
            _context.PaymentPackages.Add(Package);
        }
        else
        {
            _context.PaymentPackages.Update(Package);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = IsCreate
            ? "Gói thanh toán đã được tạo thành công."
            : "Gói thanh toán đã được cập nhật thành công.";

        return RedirectToPage("Index");
    }
}
