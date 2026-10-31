namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Models;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record OrderShopData(Guid Id, ShopStatus Status);
public sealed record OrderProductData(Guid Id, Guid ShopId, string Name, ProductStatus Status, int MinOrderQty, int? MaxOrderQty, decimal BasePrice);
public sealed record OrderVariantData(Guid Id, Guid ProductId, string Name, string Sku, int StockQty, decimal PriceAdjustment);
public sealed record OrderPromotionData(Guid Id, Guid? ProductId, int? MinQty, int? MaxQty, decimal DiscountValue,
    DateTime? StartDate, DateTime? EndDate, DiscountType DiscountType);
public sealed record OrderItemData(Guid ProductId, Guid VariantId, string ProductName, string VariantName, string Sku,
    int Quantity, decimal UnitPrice, decimal DiscountAmount, decimal Subtotal);
public sealed record CreateOrderData(Guid CustomerId, Guid ShopId, string IdempotencyKey, string RequestHash,
    string OrderNumber, decimal Subtotal, decimal DiscountAmount, decimal TotalAmount, string? Notes,
    string DeliveryAddress, string RecipientName, string RecipientPhone, DateOnly? DeliveryDateExpected,
    IReadOnlyList<OrderItemData> Items);
public sealed record OrderLookupData(Guid CustomerId, Guid ShopId, OrderStatus Status, string RequestHash, OrderResponse Response);
