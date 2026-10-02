namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for Supplier aggregate.</summary>
public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync(bool? isVerified = null, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
