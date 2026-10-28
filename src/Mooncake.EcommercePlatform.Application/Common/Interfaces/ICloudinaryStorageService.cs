namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;

/// <summary>Uploads content to Cloudinary without accessing the database.</summary>
public interface ICloudinaryStorageService
{
    /// <summary>Uploads an image with the requested access policy. The caller owns the input stream.</summary>
    Task<CloudinaryUploadResponse> UploadImageAsync(Stream content, string fileName, bool isPrivate,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads a raw file with private access. The caller owns the input stream.</summary>
    Task<CloudinaryUploadResponse> UploadFileAsync(Stream content, string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads a private raw document of any format, subject to the configured byte limit.</summary>
    Task<CloudinaryUploadResponse> UploadRawFileAsync(Stream content, string fileName,
        CancellationToken cancellationToken = default);
}
