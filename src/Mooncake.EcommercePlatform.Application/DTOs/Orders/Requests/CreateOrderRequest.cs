namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;

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

    [Required, Phone, MaxLength(25)]
    [RegularExpression(@"^(?:\+?84|0)[\s.\-()]*[35789](?:[\s.\-()]*[0-9]){8}$", ErrorMessage = "Phone number is invalid.")]
    public string RecipientPhone { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DateOnly? DeliveryDateExpected { get; init; }

    [MaxLength(40)]
    public string? ShippingZoneCode { get; init; }

    [MaxLength(64)]
    public string? AcceptedPriceVersion { get; init; }

    public bool? AcceptedTerms { get; init; }

    /// <summary>Optional custom branding and packaging brief attached to this order.</summary>
    public CreateCustomPackagingRequest? CustomPackaging { get; init; }
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
