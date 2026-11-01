namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class OrderCreationWorkflowTests
{
    private readonly Mock<IOrderRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Shop shop = new() { Id = Guid.NewGuid(), Status = ShopStatus.Active };
    private readonly Product product;
    private readonly ProductVariant variant;
    private readonly DateTime now = new(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc);

    public OrderCreationWorkflowTests()
    {
        product = new Product
        {
            Id = Guid.NewGuid(), ShopId = shop.Id, Name = "Gift box", BasePrice = 100,
            MinOrderQty = 10, MaxOrderQty = 1000, Status = ProductStatus.Active
        };
        variant = new ProductVariant
        {
            Id = Guid.NewGuid(), ProductId = product.Id, ShopId = shop.Id, Name = "Eight pieces",
            Sku = "GIFT-8", PriceAdjustment = 5, StockQty = 100
        };
        clock.SetupGet(x => x.UtcNow).Returns(now);
        repository.Setup(x => x.GetShopAsync(shop.Id, default)).ReturnsAsync(shop);
        repository.Setup(x => x.GetProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default)).ReturnsAsync([product]);
        repository.Setup(x => x.GetVariantsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default)).ReturnsAsync([variant]);
        repository.Setup(x => x.GetActivePromotionsAsync(shop.Id, default)).ReturnsAsync([]);
    }

    [Fact]
    public async Task CreateAsync_UsesServerPriceAndPersistsPendingOrderAndSnapshots()
    {
        Order? savedOrder = null;
        IReadOnlyList<OrderItem>? savedItems = null;
        repository.Setup(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), default))
            .Callback<Order, IReadOnlyList<OrderItem>, CancellationToken>((order, items, _) =>
            {
                savedOrder = order;
                savedItems = items;
            }).ReturnsAsync(true);

        var service = new OrderService(repository.Object, clock.Object);
        var response = await service.CreateAsync(customerId, "order-test-001", ValidRequest());

        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Pending, savedOrder.Status);
        Assert.Equal("order-test-001", savedOrder.IdempotencyKey);
        Assert.Equal(1050m, savedOrder.TotalAmount);
        Assert.Equal(1050m, response.TotalAmount);
        var item = Assert.Single(savedItems!);
        Assert.Equal("Gift box", item.ProductNameSnapshot);
        Assert.Equal("GIFT-8", item.VariantSkuSnapshot);
        Assert.Equal(105m, item.UnitPrice);
        Assert.Equal(10, item.Quantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("invalid key with spaces")]
    public async Task CreateAsync_RejectsInvalidIdempotencyKeyBeforePersistence(string? key)
    {
        var service = new OrderService(repository.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, key, ValidRequest()));

        Assert.Equal(400, error.StatusCode);
        repository.Verify(x => x.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
        repository.Verify(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflictWhenInventoryReservationLosesRace()
    {
        repository.Setup(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), default))
            .ReturnsAsync(false);

        var service = new OrderService(repository.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(
            customerId, "order-race-001", ValidRequest()));

        Assert.Equal(409, error.StatusCode);
        Assert.Contains("Inventory changed", error.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsRequestForInactiveShop()
    {
        repository.Setup(x => x.GetShopAsync(shop.Id, default))
            .ReturnsAsync(new Shop { Id = shop.Id, Status = ShopStatus.Suspended });

        var service = new OrderService(repository.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(
            customerId, "order-suspended-01", ValidRequest()));

        Assert.Equal(409, error.StatusCode);
        repository.Verify(x => x.CreateOrderAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), default), Times.Never);
    }

    private CreateOrderRequest ValidRequest() => new()
    {
        ShopId = shop.Id,
        Items = [new CreateOrderItemRequest { ProductId = product.Id, VariantId = variant.Id, Quantity = 10 }],
        DeliveryAddress = "10 Le Loi, Ho Chi Minh City",
        RecipientName = "Nguyen Van A",
        RecipientPhone = "0912345678",
        DeliveryDateExpected = DateOnly.FromDateTime(now.AddDays(3))
    };
}
