namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for CustomPackaging aggregate.</summary>
public interface ICustomPackagingRepository
{
    Task<IEnumerable<CustomPackaging>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomPackaging?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CustomPackaging> CreateAsync(CustomPackaging packaging, CancellationToken cancellationToken = default);
    Task<CustomPackaging> UpdateAsync(CustomPackaging packaging, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
