using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public class PaymentPackage
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    [Required]
    public int DurationDays { get; set; }

    [Required]
    [MaxLength(20)]
    public string TargetType { get; set; } = "User"; // User | Admin

    public bool IsActive { get; set; } = true;
    public int DefaultPoiPriority { get; set; } = 100;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
}
