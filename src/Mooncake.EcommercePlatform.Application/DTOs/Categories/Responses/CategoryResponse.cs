namespace Mooncake.EcommercePlatform.Application.DTOs.Categories.Responses;

/// <summary>Category projection including optional subcategories.</summary>
public record CategoryResponse(
    long Id,
    long? ParentId,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAtUtc,
    List<CategoryResponse>? Subcategories = null
);
