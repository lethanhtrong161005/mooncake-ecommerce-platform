namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to create a new product under a shop.</summary>
public record CreateProductRequest
{
    [Required]
    public long ShopId { get; init; }

    public long? CategoryId { get; init; }

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Range(0, 10000000)]
    public decimal? CustomPackagingFee { get; init; }

    public bool IsActive { get; init; } = true;
}
