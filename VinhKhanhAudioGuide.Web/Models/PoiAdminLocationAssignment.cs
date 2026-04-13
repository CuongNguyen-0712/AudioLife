using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public class PoiAdminLocationAssignment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LocationId { get; set; } = string.Empty;
}
