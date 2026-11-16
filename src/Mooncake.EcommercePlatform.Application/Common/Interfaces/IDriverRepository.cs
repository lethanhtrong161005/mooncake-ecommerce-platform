namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data-access contract for DriverProfile entities and driver trip records.</summary>
public interface IDriverRepository
{
    Task<DriverProfile?> GetProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<DriverProfile?> GetProfileByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<DriverProfile> CreateProfileAsync(DriverProfile profile, CancellationToken cancellationToken = default);

    Task<DriverProfile> UpdateProfileAsync(DriverProfile profile, CancellationToken cancellationToken = default);

    Task<PagedResult<Delivery>> GetDriverTripsPageAsync(
        string phone, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
}
