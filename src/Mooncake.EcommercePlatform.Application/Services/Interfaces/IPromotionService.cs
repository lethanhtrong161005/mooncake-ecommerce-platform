namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

/// <summary>Supplier promotion management use cases.</summary>
public interface IPromotionService
{
    Task<IReadOnlyList<PromotionResponse>> GetMineAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<PromotionResponse> CreateAsync(Guid supplierUserId, SavePromotionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionResponse> UpdateAsync(Guid supplierUserId, Guid promotionId, SavePromotionRequest request, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid supplierUserId, Guid promotionId, CancellationToken cancellationToken = default);
}
