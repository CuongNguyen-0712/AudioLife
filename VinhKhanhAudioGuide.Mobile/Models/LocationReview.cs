namespace VinhKhanhAudioGuide.Mobile.Models;

public class MobileLocationReviewDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class SubmitReviewRequest
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
