namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request to calculate totals, discounts, and split orders across shops before checking out.</summary>
public record CalculateOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<CartItemRequest> Items { get; init; } = [];
}
