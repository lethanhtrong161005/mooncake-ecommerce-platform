namespace Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;

/// <summary>Details of an uploaded file.</summary>
public record FileUploadResponse(
    string FileUrl,
    string FileName,
    long FileSizeBytes,
    string ContentType
);
