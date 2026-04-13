using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public enum PoiChangeRequestStatus
{
    Pending,
    InReview,
    Approved,
    Rejected
}

public enum PoiChangeTargetType
{
    Location,
    AudioGuide
}

public class PoiChangeRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string SubmittedByUsername { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string SubmittedByName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LocationId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LocationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Details { get; set; } = string.Empty;

    public PoiChangeTargetType TargetType { get; set; }

    [Required]
    [MaxLength(50)]
    public string TargetEntityId { get; set; } = string.Empty;

    [Required]
    public string ChangeSetJson { get; set; } = string.Empty;

    public PoiChangeRequestStatus Status { get; set; } = PoiChangeRequestStatus.Pending;

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    [MaxLength(500)]
    public string? ReviewNote { get; set; }
}
