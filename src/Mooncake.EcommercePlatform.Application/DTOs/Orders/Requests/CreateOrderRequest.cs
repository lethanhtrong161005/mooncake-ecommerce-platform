namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer request to place a multi-quantity order with one shop.</summary>
public sealed record CreateOrderRequest
{
    [Required]
    public Guid ShopId { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public List<CreateOrderItemRequest> Items { get; init; } = [];

    [Required, MaxLength(500)]
    public string DeliveryAddress { get; init; } = string.Empty;

    [Required, MaxLength(255)]
    public string RecipientName { get; init; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string RecipientPhone { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DateOnly? DeliveryDateExpected { get; init; }
}

/// <summary>Product variant and requested bulk quantity.</summary>
public sealed record CreateOrderItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    public Guid VariantId { get; init; }

    [Range(1, 1_000_000)]
    public int Quantity { get; init; }
}
