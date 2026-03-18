namespace VinhKhanhAudioGuide.Mobile.Models;

public class Feedback
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? LocationId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}