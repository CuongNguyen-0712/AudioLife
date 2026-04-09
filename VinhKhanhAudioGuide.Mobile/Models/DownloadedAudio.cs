namespace VinhKhanhAudioGuide.Mobile.Models;

public class DownloadedAudio
{
    public string AudioGuideId { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
    public long FileSize { get; set; }
}
