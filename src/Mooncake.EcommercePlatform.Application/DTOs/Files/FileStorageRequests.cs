namespace Mooncake.EcommercePlatform.Application.DTOs.Files;

using System.ComponentModel.DataAnnotations;

public sealed record PresignUploadRequest
{
    [Required, MaxLength(255)] public string FileName { get; init; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; init; } = string.Empty;
    [Range(1, 25_000_000)] public long FileSizeBytes { get; init; }
}

public sealed record PresignDownloadRequest
{
    [Required, MaxLength(700)] public string ObjectKey { get; init; } = string.Empty;
}

public sealed record PresignedFileResponse(string ObjectKey, string Url, DateTime ExpiresAtUtc, string HttpMethod, string? RequiredContentType, long? RequiredContentLength, string? PublicAssetUrl);
