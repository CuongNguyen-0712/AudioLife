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
    public string Language { get; set; } = string.Empty;
    public int InterruptedAtSeconds { get; set; }
    public bool IsDirectTap { get; set; }

    public int TotalDurationSeconds => Math.Max(0, AudioDuration * 60);

    public int EffectiveListenedSeconds
    {
        get
        {
            var total = TotalDurationSeconds;
            var raw = ListenedSeconds > 0
                ? ListenedSeconds
                : (int)Math.Round(total * Math.Clamp(Progress, 0d, 1d));

            if (total > 0)
            {
                return Math.Clamp(raw, 0, total);
            }

            return Math.Max(0, raw);
        }
    }

    public double NormalizedProgress
    {
        get
        {
            var total = TotalDurationSeconds;
            if (total > 0)
            {
                return Math.Clamp((double)EffectiveListenedSeconds / total, 0d, 1d);
            }

            return Math.Clamp(Progress, 0d, 1d);
        }
    }

    public int ProgressPercent => (int)Math.Round(NormalizedProgress * 100d, MidpointRounding.AwayFromZero);

    public string ListeningTimeDisplay => $"{FormatTime(EffectiveListenedSeconds)} / {FormatTime(TotalDurationSeconds)}";

    public string LanguageDisplay
    {
        get
        {
            var raw = string.IsNullOrWhiteSpace(Language) ? "vi" : Language.Trim();
            var code = raw.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
            return code.ToUpperInvariant();
        }
    }

    private static string FormatTime(int totalSeconds)
    {
        var safeSeconds = Math.Max(0, totalSeconds);
        var time = TimeSpan.FromSeconds(safeSeconds);

        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        return $"{time.Minutes}:{time.Seconds:D2}";
    }
}
