namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer request to create a signed VNPay checkout URL for a direct/bulk order.</summary>
public sealed record CreateOrderVnPayUrlRequest
{
    [Required] public Guid OrderId { get; init; }
    [Required, Url, MaxLength(2000)] public string ReturnUrl { get; init; } = string.Empty;
}

