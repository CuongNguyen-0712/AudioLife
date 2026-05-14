using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VinhKhanhAudioGuide.Web.Models;

namespace VinhKhanhAudioGuide.Web.Data;

public class LocationReview
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string LocationId { get; set; } = string.Empty;

    public Guid? UserId { get; set; } // AppUser Id


    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedBy { get; set; }

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}
