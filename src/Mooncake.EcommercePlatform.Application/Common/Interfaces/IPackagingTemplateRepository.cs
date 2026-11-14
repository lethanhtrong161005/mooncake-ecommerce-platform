namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Persistence operations for supplier packaging templates.</summary>
public interface IPackagingTemplateRepository
{
    Task<IReadOnlyList<PackagingTemplate>> GetByShopIdAsync(Guid shopId, CancellationToken ct = default);
    Task<IReadOnlyList<PackagingTemplate>> GetMineAsync(Guid actorUserId, CancellationToken ct = default);
    Task<PagedResult<PackagingTemplate>> GetMinePageAsync(Guid actorUserId, PackagingTemplatePageQueryRequest query, CancellationToken ct = default);
    Task<PackagingTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid?> GetShopIdForUserAsync(Guid actorUserId, CancellationToken ct = default);
    Task<PackagingTemplate> CreateAsync(PackagingTemplate entity, CancellationToken ct = default);
    Task<PackagingTemplate> UpdateAsync(PackagingTemplate entity, CancellationToken ct = default);
    Task DeleteAsync(PackagingTemplate entity, CancellationToken ct = default);
    Task EnsureTableAndSeedAsync(CancellationToken ct = default);
}

