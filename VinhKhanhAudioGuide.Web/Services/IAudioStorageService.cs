namespace VinhKhanhAudioGuide.Web.Services;

public class AudioUploadResult
{
    public string AudioUrl { get; set; } = string.Empty;
    public string CloudinaryAudioUrl { get; set; } = string.Empty;
    public string CloudinaryPublicId { get; set; } = string.Empty;
}

public interface IAudioStorageService
{
    Task<AudioUploadResult> UploadAudioAsync(IFormFile file, string publicIdPrefix, CancellationToken cancellationToken = default);
    Task<AudioUploadResult> UploadAudioAsync(Stream stream, string fileName, string publicIdPrefix, CancellationToken cancellationToken = default);
}
