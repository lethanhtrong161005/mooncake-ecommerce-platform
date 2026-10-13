namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Admin-managed product category data.</summary>
public sealed record SaveCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Url, MaxLength(1000)]
    public string? IconUrl { get; init; }

    public Guid? ParentId { get; init; }

    [Range(0, 100_000)]
    public int SortOrder { get; init; }
}
