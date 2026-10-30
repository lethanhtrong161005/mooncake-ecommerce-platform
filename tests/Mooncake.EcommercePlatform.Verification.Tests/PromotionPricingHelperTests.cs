namespace Mooncake.EcommercePlatform.Verification.Tests;

using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class PromotionPricingHelperTests
{
    [Fact]
    public void CalculateSelectsLowestEligiblePriceAndReturnsAppliedRule()
    {
        var productId = Guid.NewGuid();
        var percentage = Rule(productId, DiscountType.Percentage, 10, 50);
        var fixedPrice = Rule(productId, DiscountType.FixedPrice, 75, 100);

        var result = PromotionPricingHelper.Calculate(100, 120, productId,
            [percentage, fixedPrice], DateTime.UtcNow);

        Assert.Equal(75, result.DiscountedUnitPrice);
        Assert.Equal(3000, result.TotalDiscount);
        Assert.Equal(fixedPrice.Id, result.AppliedRule?.Id);
    }

    [Fact]
    public void CalculateIgnoresExpiredWrongProductAndQuantityRules()
    {
        var now = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var expired = Rule(productId, DiscountType.FixedPrice, 1, 1);
        expired.EndDate = now.AddSeconds(-1);
        var wrongProduct = Rule(Guid.NewGuid(), DiscountType.FixedPrice, 1, 1);
        var insufficientQuantity = Rule(productId, DiscountType.FixedPrice, 1, 500);

        var result = PromotionPricingHelper.Calculate(120, 100, productId,
            [expired, wrongProduct, insufficientQuantity], now);

        Assert.Equal(120, result.DiscountedUnitPrice);
        Assert.Equal(0, result.TotalDiscount);
        Assert.Null(result.AppliedRule);
    }

    [Theory]
    [InlineData(DiscountType.Percentage, 200, 0)]
    [InlineData(DiscountType.FixedAmount, 500, 0)]
    [InlineData(DiscountType.FixedPrice, -5, 0)]
    public void CalculateNeverReturnsNegativePrice(DiscountType type, decimal value, decimal expected)
    {
        var productId = Guid.NewGuid();

        var result = PromotionPricingHelper.Calculate(100, 10, productId,
            [Rule(productId, type, value, 1)], DateTime.UtcNow);

        Assert.Equal(expected, result.DiscountedUnitPrice);
    }

    private static PromotionRule Rule(Guid productId, DiscountType type, decimal value, int minQty) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = productId,
        Name = $"Rule {value}",
        DiscountType = type,
        DiscountValue = value,
        MinQty = minQty,
        IsActive = true
    };
}
