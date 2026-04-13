using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = "/Index" });
        }

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
}
