namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Request to advance or transition an order's lifecycle status.</summary>
public record UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; init; }

    public string? Reason { get; init; }
}
