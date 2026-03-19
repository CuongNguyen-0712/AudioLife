using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

public class SettingsModel : PageModel
{
    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public void OnGet()
    {
        Input = new SettingsInput
        {
            SiteName = "Vinh Khánh Audio Food Guide",
            MaxAudioSizeMb = 50,
            AllowMp3 = true,
            AllowWav = true,
            AllowAac = true,
            NotifyModeration = true
        };
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        TempData["Success"] = "Đã lưu cấu hình hệ thống.";
        return RedirectToPage();
    }

    public class SettingsInput
    {
        [Required]
        public string SiteName { get; set; } = string.Empty;

        [Range(5, 500)]
        public int MaxAudioSizeMb { get; set; }

        public bool AllowMp3 { get; set; }
        public bool AllowWav { get; set; }
        public bool AllowAac { get; set; }
        public bool NotifyModeration { get; set; }
    }
}
