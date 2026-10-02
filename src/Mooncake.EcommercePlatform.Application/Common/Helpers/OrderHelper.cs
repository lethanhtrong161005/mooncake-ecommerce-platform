namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Order and OrderItem entities to response DTOs.</summary>
public class OrderHelper : IOrderHelper
{
    public OrderResponse ToResponse(Order order)
    {
        var items = order.OrderItems?.Select(ToItemResponse).ToList() ?? [];

        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Customer?.User?.FullName ?? (order.Customer?.CompanyName ?? $"Customer #{order.CustomerId}"),
            order.Customer?.User?.Phone,
            order.ShopId,
            order.Shop?.Name ?? $"Shop #{order.ShopId}",
            order.Status,
            order.Subtotal,
            order.DiscountTotal,
            order.ShippingFee,
            order.TotalAmount,
            order.DepositRequired,
            order.ReceiverName,
            order.ReceiverPhone,
            order.ShippingAddress,
            order.RequiredDeliveryDate,
            order.Note,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            items
        );
    }

    public OrderItemResponse ToItemResponse(OrderItem item) =>
        new(
            item.Id,
            item.OrderId,
            item.VariantId,
            item.Variant?.Name ?? $"Variant #{item.VariantId}",
            item.Variant?.Sku,
            item.PromotionRuleId,
            item.PromotionRule?.Name,
            item.CustomPackagingId,
            item.CustomPackaging?.Name,
            item.CustomPackaging?.LogoUrl,
            item.Quantity,
            item.UnitPrice,
            item.PackagingFee,
            item.DiscountAmount,
            item.LineTotal
        );
}
