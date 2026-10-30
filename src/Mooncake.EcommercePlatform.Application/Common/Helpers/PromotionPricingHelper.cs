namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Calculates the best valid quantity discount for one order line.</summary>
public static class PromotionPricingHelper
{
    /// <summary>Evaluates all eligible rules and returns the lowest safe unit price.</summary>
    public static PromotionPriceResult Calculate(
        decimal originalUnitPrice,
        int quantity,
        Guid productId,
        IReadOnlyList<PromotionRule> rules,
        DateTime nowUtc)
    {
        var normalizedPrice = decimal.Round(Math.Max(0, originalUnitPrice), 2, MidpointRounding.AwayFromZero);
        var bestPrice = normalizedPrice;
        PromotionRule? bestRule = null;

        foreach (var rule in rules)
        {
            if (!IsEligible(rule, productId, quantity, nowUtc))
                continue;

            var candidate = rule.DiscountType switch
            {
                DiscountType.Percentage => normalizedPrice *
                    (1 - Math.Clamp(rule.DiscountValue, 0, 100) / 100m),
                DiscountType.FixedAmount => normalizedPrice - Math.Max(0, rule.DiscountValue),
                DiscountType.FixedPrice => rule.DiscountValue,
                _ => normalizedPrice
            };
            candidate = decimal.Round(Math.Clamp(candidate, 0, normalizedPrice), 2,
                MidpointRounding.AwayFromZero);
            if (candidate >= bestPrice)
                continue;

            bestPrice = candidate;
            bestRule = rule;
        }

        var discountPerUnit = decimal.Round(normalizedPrice - bestPrice, 2,
            MidpointRounding.AwayFromZero);
        var totalDiscount = decimal.Round(discountPerUnit * quantity, 2,
            MidpointRounding.AwayFromZero);
        return new PromotionPriceResult(normalizedPrice, bestPrice, totalDiscount, bestRule);
    }

    private static bool IsEligible(PromotionRule rule, Guid productId, int quantity, DateTime nowUtc) =>
        rule.IsActive && !rule.IsDeleted &&
        (!rule.ProductId.HasValue || rule.ProductId.Value == productId) &&
        (!rule.MinQty.HasValue || quantity >= rule.MinQty.Value) &&
        (!rule.MaxQty.HasValue || quantity <= rule.MaxQty.Value) &&
        (!rule.StartDate.HasValue || rule.StartDate.Value <= nowUtc) &&
        (!rule.EndDate.HasValue || rule.EndDate.Value > nowUtc);
}

/// <summary>Best-price calculation result for one product variant.</summary>
public sealed record PromotionPriceResult(
    decimal OriginalUnitPrice,
    decimal DiscountedUnitPrice,
    decimal TotalDiscount,
    PromotionRule? AppliedRule);
