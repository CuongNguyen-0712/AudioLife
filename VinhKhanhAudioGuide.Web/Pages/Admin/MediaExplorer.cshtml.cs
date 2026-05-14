using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

[Authorize(Roles = "SystemAdmin")]
public class MediaExplorerModel : PageModel
{
    private readonly IAudioStorageService _audioStorageService;
    private readonly AppDbContext _context;

    public MediaExplorerModel(IAudioStorageService audioStorageService, AppDbContext context)
    {
        _audioStorageService = audioStorageService;
        _context = context;
    }

    public List<MediaAssetViewModel> Assets { get; set; } = new();

    public async Task OnGetAsync()
    {
        ViewData["ActivePage"] = "MediaExplorer";
        var cloudinaryAssets = await _audioStorageService.ListAssetsAsync();
        
        var usedPublicIds = _context.AudioGuides
            .Where(ag => ag.CloudinaryPublicId != null)
            .Select(ag => ag.CloudinaryPublicId)
            .ToHashSet();

        Assets = cloudinaryAssets.Select(a => new MediaAssetViewModel
        {
            PublicId = a.PublicId,
            SecureUrl = a.SecureUrl,
            Format = a.Format,
            CreatedAt = a.CreatedAt,
            Bytes = a.Bytes,
            ResourceType = a.ResourceType,
            IsOrphan = !usedPublicIds.Contains(a.PublicId)
        }).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId)) return Page();

        await _audioStorageService.DeleteAssetAsync(publicId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAllOrphansAsync()
    {
        var cloudinaryAssets = await _audioStorageService.ListAssetsAsync();
        var usedPublicIds = _context.AudioGuides
            .Where(ag => ag.CloudinaryPublicId != null)
            .Select(ag => ag.CloudinaryPublicId)
            .ToHashSet();

        var orphans = cloudinaryAssets.Where(a => !usedPublicIds.Contains(a.PublicId)).ToList();

        foreach (var orphan in orphans)
        {
            await _audioStorageService.DeleteAssetAsync(orphan.PublicId);
        }

        return RedirectToPage();
    }
}

public class MediaAssetViewModel : CloudinaryAssetDto
{
    public bool IsOrphan { get; set; }
}
