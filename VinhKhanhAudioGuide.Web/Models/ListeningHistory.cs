using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class ListeningHistory
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AudioGuideId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LocationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AudioTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LocationName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string LocationImageUrl { get; set; } = string.Empty;

    public int AudioDuration { get; set; }

    public decimal Progress { get; set; }

    public int ListenedSeconds { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime LastListenedAtUtc { get; set; }

    [ForeignKey(nameof(AudioGuideId))]
    public AudioGuide? AudioGuide { get; set; }

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
}