namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for Shop aggregate.</summary>
public interface IShopRepository
{
    Task<IEnumerable<Shop>> GetAllActiveAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<Shop?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Shop?> GetBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<Shop> CreateAsync(Shop shop, CancellationToken cancellationToken = default);
    Task<Shop> UpdateAsync(Shop shop, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
