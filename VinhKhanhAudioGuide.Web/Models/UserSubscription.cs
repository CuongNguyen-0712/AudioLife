using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class UserSubscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey(nameof(AppUser))]
    public Guid? UserId { get; set; }

    [ForeignKey(nameof(AuthUser))]
    public int? AuthUserId { get; set; }

    [Required]
    [MaxLength(50)]
    [ForeignKey(nameof(PaymentPackage))]
    public string PackageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending | Active | Expired | Cancelled

    public DateTime PurchasedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? StartsAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    [MaxLength(500)]
    public string? PaymentReference { get; set; }

    /// <summary>Số tiền thực tế đã thanh toán (snapshot tại thời điểm mua)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual AppUser? User { get; set; }

    [ForeignKey(nameof(AuthUserId))]
    public virtual AuthUserAccount? AuthUser { get; set; }

    [ForeignKey(nameof(PackageId))]
    public virtual PaymentPackage? Package { get; set; }
}
