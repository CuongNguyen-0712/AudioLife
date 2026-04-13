using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class Location
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

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public int Duration { get; set; }

    [MaxLength(50)]
    public string CategoryId { get; set; } = string.Empty;

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public ICollection<AudioGuide> AudioGuides { get; set; } = new List<AudioGuide>();

    public ICollection<TourLocation> TourLocations { get; set; } = new List<TourLocation>();

    public ICollection<ListeningHistory> ListeningHistories { get; set; } = new List<ListeningHistory>();
}
