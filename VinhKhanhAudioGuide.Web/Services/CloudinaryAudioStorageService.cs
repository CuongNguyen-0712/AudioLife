using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Web.Configuration;

namespace VinhKhanhAudioGuide.Web.Services;

public class CloudinaryAudioStorageService : IAudioStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryAudioStorageService(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.CloudName) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary config thiếu. Hãy cấu hình Cloudinary:CloudName, ApiKey, ApiSecret.");
        }

        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<AudioUploadResult> UploadAudioAsync(IFormFile file, string publicIdPrefix, CancellationToken cancellationToken = default)
    {
        // Upload file audio từ form lên Cloudinary và trả URL phát chuẩn f_mp3.
        // Thuộc flow CRUD AudioGuide khi admin upload thủ công.
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("File audio rỗng.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = _options.AudioFolder,
            PublicId = $"{publicIdPrefix}-{Guid.NewGuid():N}",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = false,
            // You can optionally add Eager transformation to generate MP3 in background:
            // EagerTransforms = new List<Transformation> { new Transformation().FetchFormat("mp3") }
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Upload Cloudinary thất bại: {result.Error.Message}");
        }

        if (string.IsNullOrWhiteSpace(result.SecureUrl?.ToString()))
        {
            throw new InvalidOperationException("Cloudinary không trả về URL hợp lệ.");
        }

        // Apply on-the-fly transformation 'f_mp3' directly to the URL
        // From: https://res.cloudinary.com/dex6q1cqh/video/upload/v1...
        // To:   https://res.cloudinary.com/dex6q1cqh/video/upload/f_mp3/v1...
        var finalUrl = result.SecureUrl.ToString().Replace("/video/upload/", "/video/upload/f_mp3/");

        return new AudioUploadResult
        {
            AudioUrl = finalUrl,
            CloudinaryAudioUrl = finalUrl,
            CloudinaryPublicId = result.PublicId
        };
    }

    public async Task<AudioUploadResult> UploadAudioAsync(Stream stream, string fileName, string publicIdPrefix, CancellationToken cancellationToken = default)
    {
        // Upload audio từ stream (thường là output TTS) lên Cloudinary.
        // Thuộc flow tự động tạo audio và lưu URL về database.
        if (stream.Length <= 0)
        {
            throw new InvalidOperationException("Audio stream rỗng.");
        }

        stream.Position = 0;

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = _options.AudioFolder,
            PublicId = $"{publicIdPrefix}-{Guid.NewGuid():N}",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Upload Cloudinary thất bại: {result.Error.Message}");
        }

        if (string.IsNullOrWhiteSpace(result.SecureUrl?.ToString()))
        {
            throw new InvalidOperationException("Cloudinary không trả về URL hợp lệ.");
        }

        var finalUrl = result.SecureUrl.ToString().Replace("/video/upload/", "/video/upload/f_mp3/");
        return new AudioUploadResult
        {
            AudioUrl = finalUrl,
            CloudinaryAudioUrl = finalUrl,
            CloudinaryPublicId = result.PublicId
        };
    }

    public async Task<List<CloudinaryAssetDto>> ListAssetsAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var listParams = new ListResourcesByPrefixParams
        {
            ResourceType = ResourceType.Video,
            Type = "upload",
            Prefix = prefix ?? _options.AudioFolder,
            MaxResults = 500
        };

        var result = await _cloudinary.ListResourcesAsync(listParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException($"Lỗi lấy danh sách asset: {result.Error.Message}");
        }

        return result.Resources.Select(r => new CloudinaryAssetDto
        {
            PublicId = r.PublicId,
            SecureUrl = r.SecureUrl?.ToString() ?? string.Empty,
            Format = r.Format,
            CreatedAt = DateTime.TryParse(r.CreatedAt, out var dt) ? dt : DateTime.MinValue,
            Bytes = r.Bytes,
            ResourceType = r.ResourceType
        }).ToList();
    }

    public async Task<bool> DeleteAssetAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var delParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video
        };

        var result = await _cloudinary.DestroyAsync(delParams);
        return result.Result == "ok";
    }
}
