using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public class AppUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string QrCodeValue { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;


    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Active"; // Active | Blocked

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? CurrentActivity { get; set; }

    public DateTime? CurrentActivityAtUtc { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<UserAppSession> AppSessions { get; set; } = new List<UserAppSession>();
    public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();
    public ICollection<AppUserActivityLog> ActivityLogs { get; set; } = new List<AppUserActivityLog>();
}
