namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;

/// <summary>Custom packaging request, mockup submission, and customer review use cases.</summary>
public interface ICustomPackagingService
{
    Task<CustomPackagingResponse> CreateAsync(Guid customerId, Guid orderId,
        CreateCustomPackagingRequest request, CancellationToken cancellationToken = default);
    Task<CustomPackagingResponse> GetAsync(Guid userId, bool supplier, Guid orderId,
        CancellationToken cancellationToken = default);
    Task<CustomPackagingResponse> SubmitRevisionAsync(Guid supplierUserId, Guid orderId,
        SubmitPackagingRevisionRequest request, CancellationToken cancellationToken = default);
    Task<PackagingReviewResponse> ReviewRevisionAsync(Guid customerId, Guid orderId,
        Guid revisionId, ReviewPackagingRevisionRequest request,
        CancellationToken cancellationToken = default);
}
