namespace Mooncake.EcommercePlatform.Application.DTOs.Products.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to update the inventory stock quantity for a variant.</summary>
public record UpdateStockRequest
{
    [Required]
    [Range(0, 1000000)]
    public int StockQuantity { get; init; }
}
