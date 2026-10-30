namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps order entities to immutable API response records.</summary>
public static class OrderResponseHelper
{
    public static OrderResponse ToResponse(Order order, IReadOnlyList<OrderItem> items,
        CustomPackaging? packaging = null, int revisionCount = 0) =>
        new(order.Id, order.OrderNumber, order.CustomerId, order.ShopId, order.Status, order.Subtotal, order.DiscountAmount,
            order.TaxAmount, order.ShippingFee, order.PackagingFee, order.TotalAmount, order.DeliveryAddress, order.RecipientName,
            order.RecipientPhone, order.DeliveryDateExpected, order.Notes, order.CreatedAt,
            items.Select(item => new OrderItemResponse(item.ProductId, item.VariantId, item.ProductNameSnapshot,
                item.VariantNameSnapshot, item.VariantSkuSnapshot, item.Quantity, item.UnitPrice, item.DiscountAmount, item.Subtotal)).ToList(),
            packaging is null ? null : new CustomPackagingSummaryResponse(packaging.Id,
                packaging.CompanyName ?? string.Empty, packaging.CompanyLogoUrl, packaging.BoxDesignUrl,
                packaging.PackagingType ?? string.Empty, packaging.Status, revisionCount));
}
