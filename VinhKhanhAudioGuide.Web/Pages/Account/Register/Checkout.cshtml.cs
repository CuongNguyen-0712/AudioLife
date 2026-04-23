using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Pages.Account.Register;

public class CheckoutModel : PageModel
{
    private readonly AppDbContext _db;

    public CheckoutModel(AppDbContext db)
    {
        _db = db;
    }

    public PoiRegistrationRequest? Registration { get; set; }
    public PaymentPackage? Package { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public Guid RegId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid regId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        var reg = await LoadRegistrationAsync(regId, cancellationToken);
        if (reg is null)
            return RedirectToPage("/Account/Register/SelectPlan");

        Registration = reg;
        Package = reg.Package;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var reg = await LoadRegistrationAsync(RegId, cancellationToken);
        if (reg is null)
            return RedirectToPage("/Account/Register/SelectPlan");

        Registration = reg;
        Package = reg.Package;

        // Mock payment: mark as paid immediately
        reg.Status = PoiRegistrationStatus.PendingSetup;
        reg.PaidAtUtc = DateTime.UtcNow;
        var shortId = RegId.ToString("N")[..8].ToUpper();
        reg.PaymentReference = $"DEMO-{DateTime.UtcNow:yyyyMMddHHmmss}-{shortId}";
        // Extend expiry for setup step
        reg.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        await _db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Account/Register/SetupAccount", new { regId = RegId });
    }

    private async Task<PoiRegistrationRequest?> LoadRegistrationAsync(Guid id, CancellationToken ct)
    {
        return await _db.PoiRegistrationRequests
            .Include(r => r.Package)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.Status == PoiRegistrationStatus.AwaitingPayment &&
                r.ExpiresAtUtc > DateTime.UtcNow,
                ct);
    }
}
