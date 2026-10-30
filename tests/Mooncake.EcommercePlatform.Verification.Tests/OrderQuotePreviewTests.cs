namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class OrderQuotePreviewTests
{
    [Fact]
    public async Task PreviewUsesServerPricesAndBestPromotion()
    {
        var fixture = CreateFixture(stock: 200);
        var fixedPrice = new PromotionRule
        {
            Id = Guid.NewGuid(), ShopId = fixture.Shop.Id, ProductId = fixture.Product.Id,
            Name = "Enterprise tier", MinQty = 100, DiscountType = DiscountType.FixedPrice,
            DiscountValue = 80, IsActive = true
        };
        fixture.Repository.Setup(item => item.GetActivePromotionsAsync(fixture.Shop.Id, default))
            .ReturnsAsync([fixedPrice]);

        var response = await fixture.Service.PreviewQuoteAsync(Request(fixture, 100));

        Assert.Equal(10500, response.Subtotal);
        Assert.Equal(2500, response.TotalDiscount);
        Assert.Equal(8000, response.FinalTotal);
        Assert.Equal(fixedPrice.Id, response.LineBreakdown.Single().AppliedPromotion?.Id);
    }

    [Fact]
    public async Task PreviewRejectsQuantityAboveCurrentStock()
    {
        var fixture = CreateFixture(stock: 20);
        fixture.Repository.Setup(item => item.GetActivePromotionsAsync(fixture.Shop.Id, default))
            .ReturnsAsync([]);

        var failure = await Assert.ThrowsAsync<HttpException>(() =>
            fixture.Service.PreviewQuoteAsync(Request(fixture, 100)));

        Assert.Equal(409, failure.StatusCode);
    }

    private static OrderQuotePreviewRequest Request(QuoteFixture fixture, int quantity) => new()
    {
        ShopId = fixture.Shop.Id,
        Items = [new CreateOrderItemRequest
        {
            ProductId = fixture.Product.Id,
            VariantId = fixture.Variant.Id,
            Quantity = quantity
        }]
    };

    private static QuoteFixture CreateFixture(int stock)
    {
        var repository = new Mock<IOrderRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(item => item.UtcNow).Returns(new DateTime(2026, 10, 29, 5, 0, 0, DateTimeKind.Utc));
        var shop = new Shop { Id = Guid.NewGuid(), Status = ShopStatus.Active };
        var product = new Product
        {
            Id = Guid.NewGuid(), ShopId = shop.Id, Name = "Corporate box", BasePrice = 100,
            MinOrderQty = 10, MaxOrderQty = 1000, Status = ProductStatus.Active
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, ShopId = shop.Id, Name = "Four cakes",
            Sku = "BOX-4", PriceAdjustment = 5, StockQty = stock
        };
        repository.Setup(item => item.GetShopAsync(shop.Id, default)).ReturnsAsync(shop);
        repository.Setup(item => item.GetProductsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(product.Id)), default))
            .ReturnsAsync([product]);
        repository.Setup(item => item.GetVariantsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(variant.Id)), default))
            .ReturnsAsync([variant]);
        return new QuoteFixture(repository, new OrderService(repository.Object, clock.Object), shop,
            product, variant);
    }

    private sealed record QuoteFixture(Mock<IOrderRepository> Repository, OrderService Service,
        Shop Shop, Product Product, ProductVariant Variant);
}
