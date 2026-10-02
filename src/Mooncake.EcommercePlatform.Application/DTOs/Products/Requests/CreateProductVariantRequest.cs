namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to add a variant to a product.</summary>
public record CreateProductVariantRequest
{
    [MaxLength(100)]
    public string? Sku { get; init; }

    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [Range(0, 100000000)]
    public decimal Price { get; init; }

    [Required]
    [Range(0, 1000000)]
    public int StockQuantity { get; init; }

    [Range(1, 10000)]
    public int MinOrderQuantity { get; init; } = 1;

    public bool IsActive { get; init; } = true;
}
