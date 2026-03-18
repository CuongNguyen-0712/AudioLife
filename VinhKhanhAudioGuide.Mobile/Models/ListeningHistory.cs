namespace VinhKhanhAudioGuide.Mobile.Models;

public class ListeningHistory
{
    public string Id { get; set; } = string.Empty;
    public string AudioGuideId { get; set; } = string.Empty;
    public string AudioTitle { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string LocationImageUrl { get; set; } = string.Empty;
    public int AudioDuration { get; set; }
    public double Progress { get; set; } // 0.0 - 1.0
    public DateTime ListenedAt { get; set; }

    // DTO equivalent properties from backend db changes
    public string UserId { get; set; } = string.Empty;
    public int ListenedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastListenedAt { get; set; }
}
