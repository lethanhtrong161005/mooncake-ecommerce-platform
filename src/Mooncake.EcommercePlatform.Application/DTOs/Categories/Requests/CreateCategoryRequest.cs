namespace Mooncake.EcommercePlatform.Application.DTOs.Categories.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to create a new category.</summary>
public record CreateCategoryRequest
{
    public long? ParentId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }
}
