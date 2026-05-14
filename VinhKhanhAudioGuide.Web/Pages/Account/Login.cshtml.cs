using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Account;

[EnableRateLimiting("fixed-login")]
public class LoginModel : PageModel
{
    private readonly IAuthUserStore _authUserStore;

    public LoginModel(IAuthUserStore authUserStore)
    {
        _authUserStore = authUserStore;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(RoleNames.SystemAdmin))
            {
                return RedirectToPage("/Admin/Index");
            }

            if (User.IsInRole(RoleNames.PoiAdmin))
            {
                return RedirectToPage("/Shop/Index");
            }

            return RedirectToPage("/Account/AccessDenied");
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            return Page();
        }

        var user = await _authUserStore.FindByCredentialsAsync(Input.Username, Input.Password, HttpContext.RequestAborted);
        if (user is null)
        {
            ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        foreach (var locationId in user.LocationIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("owned_location", locationId));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        if (RoleNames.IsSystemAdmin(user.Role))
        {
            return RedirectToPage("/Admin/Index");
        }

        if (RoleNames.IsPoiAdmin(user.Role))
        {
            return RedirectToPage("/Shop/Index");
        }

        return RedirectToPage("/Account/AccessDenied");
    }

    public class LoginInput
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
