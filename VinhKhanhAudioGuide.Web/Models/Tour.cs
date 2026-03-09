using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class Tour
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public int Duration { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsFeatured { get; set; }

    public ICollection<TourLocation> TourLocations { get; set; } = new List<TourLocation>();

    [NotMapped]
    public List<string> LocationIds { get; set; } = new();
}
