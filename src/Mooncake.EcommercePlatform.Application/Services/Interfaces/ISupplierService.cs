namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

/// <summary>Supplier onboarding and verification use cases.</summary>
public interface ISupplierService
{
    Task<SupplierProfileResponse> ApplyAsync(Guid userId, ApplySupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierProfileResponse> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierProfileResponse>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<SupplierProfileResponse> ReviewAsync(Guid profileId, Guid adminUserId, ReviewSupplierRequest request, CancellationToken cancellationToken = default);
}
