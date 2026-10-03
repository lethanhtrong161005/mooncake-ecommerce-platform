namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;

/// <summary>Contract for shop volume and tiered promotion rule configurations.</summary>
public interface IPromotionRuleService
{
    Task<IEnumerable<PromotionRuleResponse>> GetByShopIdAsync(long shopId, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<PromotionRuleResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PromotionRuleResponse> CreateAsync(CreatePromotionRuleRequest request, CancellationToken cancellationToken = default);
    Task<PromotionRuleResponse> UpdateAsync(long id, UpdatePromotionRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
