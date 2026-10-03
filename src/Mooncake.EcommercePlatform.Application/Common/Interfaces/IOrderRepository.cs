namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Data access contract for Order aggregate and line items.</summary>
public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetOrdersAsync(long? customerId = null, long? shopId = null, OrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteOrderAsync(long id, CancellationToken cancellationToken = default);
}
