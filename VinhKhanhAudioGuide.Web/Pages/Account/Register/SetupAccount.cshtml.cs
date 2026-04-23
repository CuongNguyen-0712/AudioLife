using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Account.Register;

public class SetupAccountModel : PageModel
{
    private readonly AppDbContext _db;

    public SetupAccountModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public SetupInput Input { get; set; } = new();

    public string? PackageName { get; set; }
    public string? PaymentReference { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid regId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        var reg = await LoadRegistrationAsync(regId, cancellationToken);
        if (reg is null)
            return RedirectToPage("/Account/Register/SelectPlan");

        PackageName = reg.Package?.Name ?? reg.PackageId;
        PaymentReference = reg.PaymentReference ?? "N/A";
        Input.RegId = regId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var reg = await LoadRegistrationAsync(Input.RegId, cancellationToken);
        if (reg is null)
            return RedirectToPage("/Account/Register/SelectPlan");

        PackageName = reg.Package?.Name ?? reg.PackageId;
        PaymentReference = reg.PaymentReference ?? "N/A";

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin.";
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        var normalizedUsername = Input.Username.Trim().ToLowerInvariant();

        // Check username uniqueness
        var exists = await _db.AuthUserAccounts
            .AsNoTracking()
            .AnyAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (exists)
        {
            ErrorMessage = $"Tên đăng nhập \"{normalizedUsername}\" đã được sử dụng. Vui lòng chọn tên khác.";
            return Page();
        }

        // Create POI Admin account
        var newAccount = new AuthUserAccount
        {
            Username = normalizedUsername,
            Password = Input.Password, // plaintext (consistent with existing system)
            DisplayName = Input.DisplayName.Trim(),
            Role = RoleNames.PoiAdmin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AuthUserAccounts.Add(newAccount);

        // Create initial subscription for the POI Admin
        var package = reg.Package;
        if (package != null)
        {
            var subscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                AuthUser = newAccount, // Use navigation property for automatic ID propagation
                PackageId = package.Id,
                Status = "Active",
                PurchasedAtUtc = DateTime.UtcNow,
                StartsAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(package.DurationDays),
                PaymentReference = reg.PaymentReference,
                PaidAmount = package.Price,
                LastVerifiedAtUtc = DateTime.UtcNow
            };
            
            _db.UserSubscriptions.Add(subscription);
        }

        // Mark registration as completed
        reg.Status = PoiRegistrationStatus.Completed;
        reg.CreatedUsername = normalizedUsername;

        await _db.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, newAccount.Username),
            new(ClaimTypes.Name, newAccount.DisplayName),
            new(ClaimTypes.Role, RoleNames.PoiAdmin)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToPage("/Shop/Index");
    }

    private async Task<PoiRegistrationRequest?> LoadRegistrationAsync(Guid id, CancellationToken ct)
    {
        return await _db.PoiRegistrationRequests
            .Include(r => r.Package)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                r.Status == PoiRegistrationStatus.PendingSetup &&
                r.ExpiresAtUtc > DateTime.UtcNow,
                ct);
    }

    public class SetupInput
    {
        public Guid RegId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên hiển thị.")]
        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [MaxLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$",
            ErrorMessage = "Chỉ dùng chữ cái (không dấu), số, dấu chấm, gạch ngang, gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        [MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
