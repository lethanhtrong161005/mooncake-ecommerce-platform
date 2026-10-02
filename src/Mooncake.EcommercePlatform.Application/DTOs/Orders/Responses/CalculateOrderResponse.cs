namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Complete preview of multi-shop cart calculations and required deposits.</summary>
public record CalculateOrderResponse(
    List<CalculatedShopOrderResponse> ShopOrders,
    decimal GrandSubtotal,
    decimal GrandDiscountTotal,
    decimal GrandShippingFee,
    decimal GrandTotalAmount,
    decimal GrandDepositRequired
);
