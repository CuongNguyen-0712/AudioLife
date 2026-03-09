using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class AudioGuide
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string AudioUrl { get; set; } = string.Empty;

    public string TranscriptText { get; set; } = string.Empty;

    public int Duration { get; set; }

    [MaxLength(50)]
    public string LocationId { get; set; } = string.Empty;

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [MaxLength(10)]
    public string Language { get; set; } = "vi";
}
