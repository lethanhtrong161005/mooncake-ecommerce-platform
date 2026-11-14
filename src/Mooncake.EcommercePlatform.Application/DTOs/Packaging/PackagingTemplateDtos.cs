namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging;

public sealed record PackagingTemplateImageUploadResponse(string Url, string PublicId);

public sealed record PackagingTemplatePageQueryRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 5;
    public string? Q { get; init; }
    public bool? IsActive { get; init; }
    public int? Capacity { get; init; }
    public string? Material { get; init; }
    public string? Sort { get; init; }
}

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

