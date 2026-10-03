namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>An individual line item in the cart or checkout request.</summary>
public record CartItemRequest
{
    [Required]
    public long VariantId { get; init; }

    [Required]
    [Range(1, 100000)]
    public int Quantity { get; init; }

    public long? CustomPackagingId { get; init; }
}
