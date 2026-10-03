namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps PromotionRule entities to DTOs.</summary>
public class PromotionRuleHelper : IPromotionRuleHelper
{
    public PromotionRuleResponse ToResponse(PromotionRule rule) =>
        new(
            rule.Id,
            rule.ShopId,
            rule.ProductId,
            rule.Name,
            rule.DiscountType,
            rule.MinQuantity,
            rule.DiscountPercent,
            rule.DiscountAmount,
            rule.FreeQuantity,
            rule.StartsAt,
            rule.EndsAt,
            rule.IsActive
        );
}
