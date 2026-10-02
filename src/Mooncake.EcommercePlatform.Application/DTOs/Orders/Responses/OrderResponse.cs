namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Order details returned to customers and suppliers.</summary>
public record OrderResponse(
    long Id,
    long CustomerId,
    string CustomerName,
    string? CustomerPhone,
    long ShopId,
    string ShopName,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingFee,
    decimal TotalAmount,
    decimal DepositRequired,
    string ReceiverName,
    string ReceiverPhone,
    string ShippingAddress,
    DateOnly? RequiredDeliveryDate,
    string? Note,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<OrderItemResponse> Items
);
