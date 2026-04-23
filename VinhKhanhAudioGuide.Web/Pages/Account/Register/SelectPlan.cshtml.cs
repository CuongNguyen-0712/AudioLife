using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Account.Register;

public class SelectPlanModel : PageModel
{
    private readonly AppDbContext _db;

    public SelectPlanModel(AppDbContext db)
    {
        _db = db;
    }

    public List<PaymentPackage> Packages { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public string SelectedPackageId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // Nếu đã đăng nhập thì redirect ra ngoài
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        Packages = await _db.PaymentPackages
            .AsNoTracking()
            .Where(p => p.IsActive && p.TargetType == "Admin")
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SelectedPackageId))
        {
            ErrorMessage = "Vui lòng chọn một gói trước khi tiếp tục.";
            Packages = await _db.PaymentPackages
                .AsNoTracking()
                .Where(p => p.IsActive && p.TargetType == "Admin")
                .OrderBy(p => p.Price)
                .ToListAsync(cancellationToken);
            return Page();
        }

        var package = await _db.PaymentPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == SelectedPackageId && p.IsActive, cancellationToken);

        if (package is null)
        {
            ErrorMessage = "Gói bạn chọn không tồn tại hoặc đã ngừng hoạt động.";
            Packages = await _db.PaymentPackages
                .AsNoTracking()
                .Where(p => p.IsActive && p.TargetType == "Admin")
                .OrderBy(p => p.Price)
                .ToListAsync(cancellationToken);
            return Page();
        }

        // Tạo phiên đăng ký
        var registration = new PoiRegistrationRequest
        {
            Id = Guid.NewGuid(),
            PackageId = SelectedPackageId,
            Status = PoiRegistrationStatus.AwaitingPayment,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

        _db.PoiRegistrationRequests.Add(registration);
        await _db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Account/Register/Checkout", new { regId = registration.Id });
    }
}
