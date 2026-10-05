namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for bulk orders and stock reservations.</summary>
public interface IOrderRepository
{
    Task<Shop?> GetShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionRule>> GetActivePromotionsAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> CreateOrderAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderItem>> GetOrderItemsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetSupplierOrdersAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelPendingOrderAsync(Order order, Guid actorUserId, string reason, CancellationToken cancellationToken = default);
    Task<bool> ConfirmPendingOrderAsync(Order order, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> IsShopOwnedByUserAsync(Guid shopId, Guid userId, CancellationToken cancellationToken = default);
}
