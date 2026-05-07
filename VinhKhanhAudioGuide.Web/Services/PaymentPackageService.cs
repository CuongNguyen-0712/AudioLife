using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Web.Data;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Services;

// ─── DTOs ──────────────────────────────────────────────────────────────────

public sealed class PackageSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public int DurationDays { get; init; }
    public string TargetType { get; init; } = "User";
    public bool IsActive { get; init; }
    public int DefaultPoiPriority { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int TotalSubscriptions { get; init; }
    public int ActiveSubscriptions { get; init; }
    public int PendingSubscriptions { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class PackageUpsertDto
{
    public string? Id { get; init; }              // null = create
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public int DurationDays { get; init; }
    public string TargetType { get; init; } = "User";
    public bool IsActive { get; init; } = true;
    public int DefaultPoiPriority { get; init; } = 100;
}

public sealed class PackageDashboardStatsDto
{
    public int TotalPackages { get; init; }
    public int ActivePackages { get; init; }
    public int InactivePackages { get; init; }
    public int TotalActiveSubscriptions { get; init; }
    public int TotalPendingSubscriptions { get; init; }
    public decimal TotalRevenue { get; init; }
    public PackageSummaryDto? MostPopularPackage { get; init; }
}

// ─── Interface ─────────────────────────────────────────────────────────────

public interface IPaymentPackageService
{
    Task<List<PackageSummaryDto>> GetAllAsync(string? search = null, bool? activeOnly = null, string? targetType = null, CancellationToken ct = default);
    Task<PackageSummaryDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PackageDashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<PaymentPackage> CreateAsync(PackageUpsertDto dto, CancellationToken ct = default);
    Task<PaymentPackage?> UpdateAsync(PackageUpsertDto dto, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(string id, CancellationToken ct = default);
    Task<(bool Success, string? Error)> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}

// ─── Implementation ─────────────────────────────────────────────────────────

public class PaymentPackageService : IPaymentPackageService
{
    private readonly AppDbContext _db;

    public PaymentPackageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PackageSummaryDto>> GetAllAsync(
        string? search = null,
        bool? activeOnly = null,
        string? targetType = null,
        CancellationToken ct = default)
    {
        // Lấy danh sách gói thanh toán kèm thống kê subscription/revenue.
        // Thuộc flow dashboard và quản trị package CRUD.
        var query = _db.PaymentPackages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Id, $"%{term}%") ||
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.Description ?? string.Empty, $"%{term}%"));
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(p => p.IsActive == activeOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query = query.Where(p => p.TargetType == targetType);
        }

        var packages = await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Price)
            .ToListAsync(ct);

        // Load subscription stats in bulk
        var packageIds = packages.Select(p => p.Id).ToList();
        var subs = await _db.UserSubscriptions
            .AsNoTracking()
            .Where(s => packageIds.Contains(s.PackageId))
            .GroupBy(s => s.PackageId)
            .Select(g => new
            {
                PackageId = g.Key,
                Total = g.Count(),
                Active = g.Count(s => s.Status == "Active"),
                Pending = g.Count(s => s.Status == "Pending"),
                Revenue = g.Where(s => s.Status == "Active" || s.Status == "Expired")
                           .Sum(s => (decimal?)s.PaidAmount ?? 0)
            })
            .ToListAsync(ct);

        var subLookup = subs.ToDictionary(s => s.PackageId);

        return packages.Select(p =>
        {
            subLookup.TryGetValue(p.Id, out var stat);
            return new PackageSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                DurationDays = p.DurationDays,
                TargetType = p.TargetType,
                IsActive = p.IsActive,
                DefaultPoiPriority = p.DefaultPoiPriority,
                CreatedAtUtc = p.CreatedAtUtc,
                TotalSubscriptions = stat?.Total ?? 0,
                ActiveSubscriptions = stat?.Active ?? 0,
                PendingSubscriptions = stat?.Pending ?? 0,
                Revenue = stat?.Revenue ?? 0
            };
        }).ToList();
    }

    public async Task<PackageSummaryDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var results = await GetAllAsync(search: id, ct: ct);
        return results.FirstOrDefault(p => p.Id == id);
    }

    public async Task<PackageDashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        // Tổng hợp KPI package (active, pending, revenue, gói phổ biến nhất).
        // Dùng cho màn hình tổng quan PaymentPackages admin.
        var allPackages = await GetAllAsync(ct: ct);

        var totalActive = allPackages.Sum(p => p.ActiveSubscriptions);
        var totalPending = allPackages.Sum(p => p.PendingSubscriptions);
        var totalRevenue = allPackages.Sum(p => p.Revenue);
        var mostPopular = allPackages
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.ActiveSubscriptions)
            .ThenByDescending(p => p.TotalSubscriptions)
            .FirstOrDefault();

        return new PackageDashboardStatsDto
        {
            TotalPackages = allPackages.Count,
            ActivePackages = allPackages.Count(p => p.IsActive),
            InactivePackages = allPackages.Count(p => !p.IsActive),
            TotalActiveSubscriptions = totalActive,
            TotalPendingSubscriptions = totalPending,
            TotalRevenue = totalRevenue,
            MostPopularPackage = mostPopular
        };
    }

    public async Task<PaymentPackage> CreateAsync(PackageUpsertDto dto, CancellationToken ct = default)
    {
        // Tạo mới gói thanh toán từ dữ liệu form admin.
        // Thuộc flow CRUD Create package.
        var entity = new PaymentPackage
        {
            Id = (dto.Id ?? Guid.NewGuid().ToString("N")[..12]).Trim().ToLowerInvariant(),
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Price = dto.Price,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            DurationDays = dto.DurationDays,
            TargetType = dto.TargetType ?? "User",
            IsActive = dto.IsActive,
            DefaultPoiPriority = dto.DefaultPoiPriority,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.PaymentPackages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<PaymentPackage?> UpdateAsync(PackageUpsertDto dto, CancellationToken ct = default)
    {
        // Cập nhật thông tin gói hiện có (giá, thời hạn, trạng thái).
        // Thuộc flow CRUD Update package.
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;

        var entity = await _db.PaymentPackages.FindAsync(new object[] { dto.Id }, ct);
        if (entity is null) return null;

        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.Price = dto.Price;
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.DurationDays = dto.DurationDays;
        entity.TargetType = dto.TargetType ?? "User";
        entity.IsActive = dto.IsActive;
        entity.DefaultPoiPriority = dto.DefaultPoiPriority;

        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> ToggleActiveAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.PaymentPackages.FindAsync(new object[] { id }, ct);
        if (entity is null) return false;

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(string id, CancellationToken ct = default)
    {
        // Xóa gói nếu không còn subscription Active/Pending để tránh phá dữ liệu nghiệp vụ.
        // Thuộc flow CRUD Delete package có kiểm tra ràng buộc.
        var entity = await _db.PaymentPackages
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (entity is null)
            return (false, "Không tìm thấy gói.");

        if (entity.Subscriptions.Any(s => s.Status is "Active" or "Pending"))
            return (false, "Không thể xóa gói này vì còn có đăng ký đang hoạt động.");

        _db.PaymentPackages.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        return await _db.PaymentPackages.AnyAsync(p => p.Id == id, ct);
    }
}
