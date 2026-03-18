namespace VinhKhanhAudioGuide.Mobile.Models;

public class Location
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Duration { get; set; } // Duration in minutes
    public string CategoryId { get; set; } = string.Empty;
    public List<AudioGuide> AudioGuides { get; set; } = new();
    public List<Feedback> Feedbacks { get; set; } = new();

    // Computed properties for UI binding
    public int AudioGuideCount => AudioGuides.Count;
    public string CategoryName { get; set; } = string.Empty;
}
