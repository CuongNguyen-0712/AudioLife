using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class AppUserActivityLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ActivityName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ActivityContext { get; set; }

    [MaxLength(200)]
    public string? Route { get; set; }

    public bool IsForeground { get; set; }

    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual AppUser? User { get; set; }
}