namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.CustomPackagings.Responses;

/// <summary>Contract for managing custom gift packaging and logo printing designs.</summary>
public interface ICustomPackagingService
{
    Task<IEnumerable<CustomPackagingResponse>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default);
    Task<CustomPackagingResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CustomPackagingResponse> CreateAsync(CreateCustomPackagingRequest request, CancellationToken cancellationToken = default);
    Task<CustomPackagingResponse> UpdateAsync(long id, UpdateCustomPackagingRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
