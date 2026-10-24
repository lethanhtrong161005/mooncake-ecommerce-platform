namespace Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;

/// <summary>Upload metadata; the caller may persist Url in an existing business entity.</summary>
public sealed record CloudinaryUploadResponse(
    string Url, string PublicId, string ResourceType, string DeliveryType, long SizeBytes);
