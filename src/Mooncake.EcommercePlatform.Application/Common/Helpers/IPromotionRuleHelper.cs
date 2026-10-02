namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for PromotionRule mapping helpers.</summary>
public interface IPromotionRuleHelper
{
    PromotionRuleResponse ToResponse(PromotionRule rule);
}
