namespace VinhKhanhAudioGuide.Mobile.Models;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "vi";
    public List<string> FavoriteLocationIds { get; set; } = new();
    public List<string> VisitedLocationIds { get; set; } = new();
    public List<DownloadedAudio> DownloadedAudios { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

public class DownloadedAudio
{
    public string AudioGuideId { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
    public long FileSize { get; set; }
}
