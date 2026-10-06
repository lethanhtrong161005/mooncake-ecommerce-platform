namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Files;

public interface IFileStorageService
{
    PresignedFileResponse CreateUploadUrl(Guid ownerId, string fileName, string contentType, long fileSizeBytes);
    PresignedFileResponse CreateDownloadUrl(Guid requesterId, string objectKey, bool administrator);
}
