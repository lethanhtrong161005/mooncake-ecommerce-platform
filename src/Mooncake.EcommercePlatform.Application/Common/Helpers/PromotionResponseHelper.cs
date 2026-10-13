namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps promotion rules to supplier API responses.</summary>
public static class PromotionResponseHelper
{
    public static PromotionResponse ToResponse(PromotionRule promotion) =>
        new(promotion.Id, promotion.ShopId, promotion.ProductId, promotion.Name, promotion.Description,
            promotion.MinQty, promotion.MaxQty, promotion.DiscountType, promotion.DiscountValue,
            promotion.StartDate, promotion.EndDate, promotion.IsActive);
}
