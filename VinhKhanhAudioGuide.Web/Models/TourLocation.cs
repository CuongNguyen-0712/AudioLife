using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhAudioGuide.Web.Models;

public class TourLocation
{
    [MaxLength(50)]
    public string TourId { get; set; } = string.Empty;

    [ForeignKey(nameof(TourId))]
    public Tour? Tour { get; set; }

    [MaxLength(50)]
    public string LocationId { get; set; } = string.Empty;

    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    public int SortOrder { get; set; }
}
