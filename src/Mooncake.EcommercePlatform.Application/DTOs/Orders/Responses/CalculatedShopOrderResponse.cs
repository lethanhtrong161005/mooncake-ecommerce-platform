namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Calculated preview of an order grouped by a single shop.</summary>
public record CalculatedShopOrderResponse(
    long ShopId,
    string ShopName,
    List<CalculatedItemResponse> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingFee,
    decimal TotalAmount,
    decimal DepositRequired,
    bool IsBulkOrder
);
