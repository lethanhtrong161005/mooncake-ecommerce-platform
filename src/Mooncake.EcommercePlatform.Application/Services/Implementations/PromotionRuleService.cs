namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Promotions.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements promotion rule configuration and validations.</summary>
public class PromotionRuleService(
    IPromotionRuleRepository promotionRuleRepository,
    IShopRepository shopRepository,
    IProductRepository productRepository,
    IPromotionRuleHelper promotionRuleHelper) : IPromotionRuleService
{
    public async Task<IEnumerable<PromotionRuleResponse>> GetByShopIdAsync(long shopId, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var rules = await promotionRuleRepository.GetByShopIdAsync(shopId, activeOnly, cancellationToken);
        return rules.Select(promotionRuleHelper.ToResponse);
    }

    public async Task<PromotionRuleResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var rule = await promotionRuleRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Promotion rule with id '{id}' was not found.");
        return promotionRuleHelper.ToResponse(rule);
    }

    public async Task<PromotionRuleResponse> CreateAsync(CreatePromotionRuleRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, $"Shop with id '{request.ShopId}' was not found.");

        if (request.ProductId.HasValue)
        {
            var product = await productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken)
                          ?? throw new HttpException(404, $"Product with id '{request.ProductId.Value}' was not found.");

            if (product.ShopId != request.ShopId)
            {
                throw new HttpException(400, "The specified product does not belong to this shop.");
            }
        }

        ValidatePromotionRule(request.DiscountType, request.MinQuantity, request.DiscountPercent, request.DiscountAmount, request.FreeQuantity, request.StartsAt, request.EndsAt);

        var rule = new PromotionRule
        {
            ShopId = request.ShopId,
            ProductId = request.ProductId,
            Name = request.Name,
            DiscountType = request.DiscountType,
            MinQuantity = request.MinQuantity,
            DiscountPercent = request.DiscountPercent,
            DiscountAmount = request.DiscountAmount,
            FreeQuantity = request.FreeQuantity,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await promotionRuleRepository.CreateAsync(rule, cancellationToken);
        return promotionRuleHelper.ToResponse(created);
    }

    public async Task<PromotionRuleResponse> UpdateAsync(long id, UpdatePromotionRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await promotionRuleRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Promotion rule with id '{id}' was not found.");

        if (request.ProductId.HasValue)
        {
            var product = await productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken)
                          ?? throw new HttpException(404, $"Product with id '{request.ProductId.Value}' was not found.");

            if (product.ShopId != rule.ShopId)
            {
                throw new HttpException(400, "The specified product does not belong to this shop.");
            }
        }

        ValidatePromotionRule(request.DiscountType, request.MinQuantity, request.DiscountPercent, request.DiscountAmount, request.FreeQuantity, request.StartsAt, request.EndsAt);

        rule.ProductId = request.ProductId;
        rule.Name = request.Name;
        rule.DiscountType = request.DiscountType;
        rule.MinQuantity = request.MinQuantity;
        rule.DiscountPercent = request.DiscountPercent;
        rule.DiscountAmount = request.DiscountAmount;
        rule.FreeQuantity = request.FreeQuantity;
        rule.StartsAt = request.StartsAt;
        rule.EndsAt = request.EndsAt;
        rule.IsActive = request.IsActive;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await promotionRuleRepository.UpdateAsync(rule, cancellationToken);
        return promotionRuleHelper.ToResponse(updated);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var rule = await promotionRuleRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new HttpException(404, $"Promotion rule with id '{id}' was not found.");

        await promotionRuleRepository.DeleteAsync(rule.Id, cancellationToken);
    }

    private static void ValidatePromotionRule(DiscountType type, int minQuantity, decimal? percent, decimal? amount, int? freeQty, DateTime? start, DateTime? end)
    {
        if (minQuantity <= 0)
        {
            throw new HttpException(400, "Minimum quantity must be greater than 0.");
        }

        switch (type)
        {
            case DiscountType.Percent:
                if (!percent.HasValue || percent.Value <= 0 || percent.Value > 100)
                    throw new HttpException(400, "Discount percent must be between 0.01 and 100.");
                break;
            case DiscountType.FixedAmount:
                if (!amount.HasValue || amount.Value <= 0)
                    throw new HttpException(400, "Discount amount must be greater than 0.");
                break;
            case DiscountType.BuyXGetY:
                if (!freeQty.HasValue || freeQty.Value <= 0)
                    throw new HttpException(400, "Free quantity must be greater than 0.");
                break;
        }

        if (start.HasValue && end.HasValue && end.Value <= start.Value)
        {
            throw new HttpException(400, "Promotion end date must be after start date.");
        }
    }
}
