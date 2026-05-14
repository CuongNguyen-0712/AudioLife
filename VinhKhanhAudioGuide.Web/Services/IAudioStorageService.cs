namespace VinhKhanhAudioGuide.Web.Services;

public class AudioUploadResult
{
    public string AudioUrl { get; set; } = string.Empty;
    public string CloudinaryAudioUrl { get; set; } = string.Empty;
    public string CloudinaryPublicId { get; set; } = string.Empty;
}

public class CloudinaryAssetDto
{
    public string PublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Bytes { get; set; }
    public string ResourceType { get; set; } = string.Empty;
}

public interface IAudioStorageService
{
    Task<AudioUploadResult> UploadAudioAsync(IFormFile file, string publicIdPrefix, CancellationToken cancellationToken = default);
    Task<AudioUploadResult> UploadAudioAsync(Stream stream, string fileName, string publicIdPrefix, CancellationToken cancellationToken = default);
    Task<List<CloudinaryAssetDto>> ListAssetsAsync(string? prefix = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetAsync(string publicId, CancellationToken cancellationToken = default);
}
