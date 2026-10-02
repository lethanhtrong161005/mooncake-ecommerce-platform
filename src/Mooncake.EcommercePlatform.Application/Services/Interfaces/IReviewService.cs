namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Reviews.Responses;

/// <summary>Review submission, reputation scoring adjustment, and audit log tracking.</summary>
public interface IReviewService
{
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReviewResponse>> GetReviewsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<ReviewResponse?> GetReviewByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReputationLogResponse>> GetReputationLogsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
}
