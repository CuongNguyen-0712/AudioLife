using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;
using VinhKhanhAudioGuide.Web.Services;

namespace VinhKhanhAudioGuide.Web.Pages.Admin;

// ─────────────────────────────────────────────────────────────────────────────
// Package List Page
// ─────────────────────────────────────────────────────────────────────────────
public class PaymentPackagesModel : PageModel
{
    private readonly IPaymentPackageService _packageService;

    public PaymentPackagesModel(IPaymentPackageService packageService)
    {
        _packageService = packageService;
    }

    public List<PackageSummaryDto> Packages { get; set; } = new();
    public PackageDashboardStatsDto Stats { get; set; } = new();

    // Convenience props for legacy Razor view compatibility
    public int ActiveSubscriptionCount => Stats.TotalActiveSubscriptions;
    public int PendingSubscriptionCount => Stats.TotalPendingSubscriptions;
    public int InactivePackageCount => Stats.InactivePackages;
    public int ActivePackageCount => Stats.ActivePackages;

    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = string.Empty;
    public string TypeFilter { get; set; } = string.Empty;

    public async Task OnGetAsync(
        string searchTerm = "",
        string statusFilter = "",
        string typeFilter = "",
        CancellationToken cancellationToken = default)
    {
        SearchTerm = searchTerm;
        StatusFilter = statusFilter;
        TypeFilter = typeFilter;

        bool? activeFilter = statusFilter switch
        {
            "Active" => true,
            "Inactive" => false,
            _ => null
        };

        Packages = await _packageService.GetAllAsync(
            search: string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
            activeOnly: activeFilter,
            targetType: string.IsNullOrWhiteSpace(typeFilter) ? null : typeFilter,
            ct: cancellationToken);

        Stats = await _packageService.GetDashboardStatsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleAsync(string id, CancellationToken cancellationToken = default)
    {
        var toggled = await _packageService.ToggleActiveAsync(id, cancellationToken);
        if (!toggled)
        {
            TempData["ErrorMessage"] = "Không tìm thấy gói thanh toán.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var (success, error) = await _packageService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Không thể xóa gói thanh toán.";
        }
        else
        {
            TempData["SuccessMessage"] = "Gói thanh toán đã được xóa thành công.";
        }

        return RedirectToPage();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Package Create / Edit Page
// ─────────────────────────────────────────────────────────────────────────────
public class PaymentPackageEditModel : PageModel
{
    private readonly IPaymentPackageService _packageService;

    public PaymentPackageEditModel(IPaymentPackageService packageService)
    {
        _packageService = packageService;
    }

    [BindProperty]
    public PackageFormInput Input { get; set; } = new();

    public bool IsCreate { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id, CancellationToken cancellationToken = default)
    {
        IsCreate = string.IsNullOrEmpty(id);

        if (!IsCreate)
        {
            var existing = await _packageService.GetByIdAsync(id!, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            Input = new PackageFormInput
            {
                Id = existing.Id,
                Name = existing.Name,
                Description = existing.Description,
                Price = existing.Price,
                Currency = existing.Currency,
                DurationDays = existing.DurationDays,
                TargetType = existing.TargetType,
                IsActive = existing.IsActive
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            IsCreate = string.IsNullOrEmpty(Input.Id);
            return Page();
        }

        IsCreate = string.IsNullOrEmpty(Input.Id);

        var dto = new PackageUpsertDto
        {
            Id = string.IsNullOrWhiteSpace(Input.Id) ? null : Input.Id.Trim(),
            Name = Input.Name,
            Description = Input.Description,
            Price = Input.Price,
            Currency = Input.Currency,
            DurationDays = Input.DurationDays,
            TargetType = Input.TargetType,
            IsActive = Input.IsActive
        };

        if (IsCreate)
        {
            await _packageService.CreateAsync(dto, cancellationToken);
            TempData["SuccessMessage"] = "Gói thanh toán đã được tạo thành công.";
        }
        else
        {
            var updated = await _packageService.UpdateAsync(dto, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }
            TempData["SuccessMessage"] = "Gói thanh toán đã được cập nhật thành công.";
        }

        return RedirectToPage("Index");
    }

    public class PackageFormInput
    {
        public string? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên gói.")]
        [System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? Description { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập giá.")]
        [System.ComponentModel.DataAnnotations.Range(0, 100_000_000, ErrorMessage = "Giá không hợp lệ.")]
        public decimal Price { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(10)]
        public string Currency { get; set; } = "VND";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số ngày.")]
        [System.ComponentModel.DataAnnotations.Range(1, 3650, ErrorMessage = "Số ngày phải từ 1 đến 3650.")]
        public int DurationDays { get; set; } = 30;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn loại đối tượng.")]
        public string TargetType { get; set; } = "User";

        public bool IsActive { get; set; } = true;
    }
}
