namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Variant quantities and optional shipping zone used to calculate a server-authoritative order quote.</summary>
public sealed record OrderQuotePreviewRequest
{
    [Required(ErrorMessage = "ShopId is required.")]
    public Guid ShopId { get; init; }

    [Required(ErrorMessage = "Items list cannot be empty.")]
    [MinLength(1, ErrorMessage = "At least one item must be quoted.")]
    [MaxLength(100, ErrorMessage = "A quote cannot exceed 100 distinct items.")]
    public List<CreateOrderItemRequest> Items { get; init; } = [];

    [MaxLength(40)]
    public string? ShippingZoneCode { get; init; }
}
