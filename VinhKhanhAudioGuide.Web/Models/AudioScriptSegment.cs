using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class AudioScriptSegment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AudioGuideId { get; set; } = string.Empty;

    public int StartTimeSeconds { get; set; }
    
    public int EndTimeSeconds { get; set; }

    public string ScriptText { get; set; } = string.Empty;

    [ForeignKey(nameof(AudioGuideId))]
    public AudioGuide? AudioGuide { get; set; }
}
