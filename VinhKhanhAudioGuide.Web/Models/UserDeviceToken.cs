using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class UserDeviceToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string FCMToken { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Platform { get; set; } // Android | iOS

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastSeenAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser? User { get; set; }
}
