namespace VinhKhanhAudioGuide.Mobile.Models;

public class AudioScriptSegment
{
    public int Id { get; set; }
    public string AudioGuideId { get; set; } = string.Empty;
    public int StartTimeSeconds { get; set; }
    public int EndTimeSeconds { get; set; }
    public string ScriptText { get; set; } = string.Empty;
}