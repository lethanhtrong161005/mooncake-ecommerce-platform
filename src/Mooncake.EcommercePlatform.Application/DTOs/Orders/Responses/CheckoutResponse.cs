namespace Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Result of checking out a multi-shop order.</summary>
public record CheckoutResponse(
    List<OrderResponse> Orders,
    int TotalOrdersCreated,
    decimal GrandTotalAmount,
    decimal GrandDepositRequired
);
