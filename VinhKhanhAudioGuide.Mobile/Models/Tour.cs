namespace VinhKhanhAudioGuide.Mobile.Models;

public class Tour
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Duration { get; set; } // Duration in minutes
    public List<string> LocationIds { get; set; } = new();
    public decimal Price { get; set; }
    public bool IsFeatured { get; set; }
}
