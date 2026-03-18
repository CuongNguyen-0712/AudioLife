using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class ListeningHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AudioGuideId { get; set; } = string.Empty;

    public int ListenedSeconds { get; set; }
    
    public bool IsCompleted { get; set; }

    public DateTime LastListenedAt { get; set; }

    [ForeignKey(nameof(AudioGuideId))]
    public AudioGuide? AudioGuide { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
