using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class UserAppSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey(nameof(AppUser))]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string TokenValue { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? RefreshToken { get; set; }

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastValidatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual AppUser? User { get; set; }
}
