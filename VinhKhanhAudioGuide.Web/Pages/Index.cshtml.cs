using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace VinhKhanhAudioGuide.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = "/Index" });
        }

        if (User.IsInRole("Admin"))
        {
            return RedirectToPage("/Admin/Index");
        }

        return RedirectToPage("/Shop/Index");
    }
}
