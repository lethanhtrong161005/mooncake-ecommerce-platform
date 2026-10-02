namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for ShopTemplate aggregate.</summary>
public interface IShopTemplateRepository
{
    Task<IEnumerable<ShopTemplate>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<ShopTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShopTemplate> CreateAsync(ShopTemplate template, CancellationToken cancellationToken = default);
    Task<ShopTemplate> UpdateAsync(ShopTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
