namespace VinhKhanhAudioGuide.Mobile.Models;

public class AudioGuide
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public string? CloudinaryAudioUrl { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public int Duration { get; set; } // Duration in minutes
    public string LocationId { get; set; } = string.Empty;
    public string Language { get; set; } = "vi"; // Default Vietnamese
    public List<AudioScriptSegment> ScriptSegments { get; set; } = new();
    public List<ListeningHistory> ListeningHistories { get; set; } = new();
}
