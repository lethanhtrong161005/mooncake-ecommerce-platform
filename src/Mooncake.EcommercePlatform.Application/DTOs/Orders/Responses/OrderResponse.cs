namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Order summary and immutable line-item snapshots.</summary>
public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid ShopId,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingFee,
    decimal TotalAmount,
    string? DeliveryAddress,
    string? RecipientName,
    string? RecipientPhone,
    DateOnly? DeliveryDateExpected,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

/// <summary>Order line using product and SKU snapshots captured at checkout.</summary>
public sealed record OrderItemResponse(Guid ProductId, Guid VariantId, string ProductName, string VariantName, string Sku, int Quantity, decimal UnitPrice, decimal DiscountAmount, decimal Subtotal);
