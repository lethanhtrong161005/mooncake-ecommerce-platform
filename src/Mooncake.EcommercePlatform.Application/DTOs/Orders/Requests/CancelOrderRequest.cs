namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer cancellation reason for an unconfirmed order.</summary>
public sealed record CancelOrderRequest
{
    [Required, MinLength(3), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}
