using System.ComponentModel.DataAnnotations;

namespace VinhKhanhAudioGuide.Web.Models;

public class Category
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Icon { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
