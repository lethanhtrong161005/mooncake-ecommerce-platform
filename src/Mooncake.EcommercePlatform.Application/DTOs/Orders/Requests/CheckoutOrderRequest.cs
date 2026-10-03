namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to place orders with automatic multi-shop splitting, promotion calculation, and stock decrement.</summary>
public record CheckoutOrderRequest
{
    [Required]
    public long CustomerId { get; init; }

    [Required]
    [MaxLength(255)]
    public string ReceiverName { get; init; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public string ShippingAddress { get; init; } = string.Empty;

    public DateOnly? RequiredDeliveryDate { get; init; }

    public string? Note { get; init; }

    [Required]
    [MinLength(1)]
    public List<CartItemRequest> Items { get; init; } = [];
}
