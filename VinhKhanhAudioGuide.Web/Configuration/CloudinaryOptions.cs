namespace VinhKhanhAudioGuide.Web.Configuration;

public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string AudioFolder { get; set; } = "audio";
}
