namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Contract for pricing calculations, multi-shop order splitting, order lifecycle, and deposit management.</summary>
public interface IOrderService
{
    Task<CalculateOrderResponse> CalculateAsync(CalculateOrderRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutResponse> CheckoutAsync(CheckoutOrderRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderResponse>> GetOrdersAsync(long? customerId = null, long? shopId = null, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetOrderByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<OrderResponse> UpdateStatusAsync(long id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse> PayDepositAsync(long id, PayDepositRequest request, CancellationToken cancellationToken = default);
}
