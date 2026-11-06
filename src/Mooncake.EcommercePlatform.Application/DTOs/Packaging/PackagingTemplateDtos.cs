namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging;

public sealed record PackagingTemplateResponse(
    Guid Id,
    Guid ShopId,
    string Name,
    string? Description,
    string? Material,
    string? Dimensions,
    int Capacity,
    decimal BasePrice,
    string? SampleImageUrl,
    string? Technique,
    int MinOrderQty,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public sealed record CreatePackagingTemplateRequest(
    string Name,
    string? Description,
    string? Material,
    string? Dimensions,
    int Capacity,
    decimal BasePrice,
    string? SampleImageUrl,
    string? Technique,
    int MinOrderQty,
    int SortOrder
);

public sealed record UpdatePackagingTemplateRequest(
    string Name,
    string? Description,
    string? Material,
    string? Dimensions,
    int Capacity,
    decimal BasePrice,
    string? SampleImageUrl,
    string? Technique,
    int MinOrderQty,
    bool IsActive,
    int SortOrder
);

