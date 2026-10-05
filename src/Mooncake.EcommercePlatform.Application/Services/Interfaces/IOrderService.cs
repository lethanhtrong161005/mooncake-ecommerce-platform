namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;

/// <summary>Bulk order placement, retrieval, confirmation, and cancellation use cases.</summary>
public interface IOrderService
{
    Task<OrderResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetAsync(Guid userId, bool supplier, Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponse> CancelAsync(Guid customerId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse> ConfirmAsync(Guid supplierUserId, Guid orderId, CancellationToken cancellationToken = default);
}
