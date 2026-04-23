using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

/// <summary>
/// Lưu trạng thái phiên đăng ký tài khoản POI Admin (chọn gói → thanh toán → setup tài khoản).
/// Mỗi bản ghi có TTL 30 phút tính từ lúc tạo.
/// </summary>
public class PoiRegistrationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string PackageId { get; set; } = string.Empty;

    /// <summary>
    /// SelectingPlan | AwaitingPayment | PendingSetup | Completed | Cancelled
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = PoiRegistrationStatus.AwaitingPayment;

    /// <summary>Mã tham chiếu thanh toán (mock hoặc gateway reference)</summary>
    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Username đã tạo sau bước SetupAccount</summary>
    [MaxLength(100)]
    public string? CreatedUsername { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Phiên đăng ký hết hạn sau 30 phút</summary>
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);

    // Navigation
    public PaymentPackage? Package { get; set; }
}

public static class PoiRegistrationStatus
{
    public const string AwaitingPayment = "AwaitingPayment";
    public const string PendingSetup = "PendingSetup";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}
