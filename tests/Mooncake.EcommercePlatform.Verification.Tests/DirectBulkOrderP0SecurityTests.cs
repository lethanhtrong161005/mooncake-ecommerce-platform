namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Xunit;

public sealed class DirectBulkOrderP0SecurityTests
{
    private readonly DateTime fixedNow = new(2026, 11, 4, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task OrderService_CrossTenantSupplier_IsBlockedOnGetConfirmRejectProcessAndShip()
    {
        var repository = new Mock<IOrderRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        var shopAId = Guid.NewGuid();
        var customerAId = Guid.NewGuid();
        var supplierBUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var shopAOrder = new Order
        {
            Id = orderId,
            CustomerId = customerAId,
            ShopId = shopAId,
            Status = OrderStatus.Pending,
            OrderNumber = "MC-TEST-CROSS-001"
        };

        repository.Setup(r => r.GetOrderAsync(orderId, default)).ReturnsAsync(shopAOrder);
        repository.Setup(r => r.IsShopOwnedByUserAsync(shopAId, supplierBUserId, default)).ReturnsAsync(false);

        var service = new OrderService(repository.Object, clock.Object);

        var getEx = await Assert.ThrowsAsync<HttpException>(() => service.GetAsync(supplierBUserId, true, orderId));
        Assert.Equal(404, getEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, getEx.ErrorCode);

        var confirmEx = await Assert.ThrowsAsync<HttpException>(() => service.ConfirmAsync(supplierBUserId, orderId));
        Assert.Equal(404, confirmEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, confirmEx.ErrorCode);

        var rejectEx = await Assert.ThrowsAsync<HttpException>(() =>
            service.RejectAsync(supplierBUserId, orderId, new CancelOrderRequest { Reason = "Unauthorized supplier" }));
        Assert.Equal(404, rejectEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, rejectEx.ErrorCode);

        var processEx = await Assert.ThrowsAsync<HttpException>(() => service.StartProcessingAsync(supplierBUserId, orderId));
        Assert.Equal(404, processEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, processEx.ErrorCode);

        var shipEx = await Assert.ThrowsAsync<HttpException>(() => service.MarkShippedAsync(supplierBUserId, orderId));
        Assert.Equal(404, shipEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, shipEx.ErrorCode);

        repository.Verify(r => r.ConfirmPendingOrderAsync(It.IsAny<Order>(), It.IsAny<Guid>(), default), Times.Never);
        repository.Verify(r => r.CancelPendingOrderAsync(It.IsAny<Order>(), It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
        repository.Verify(r => r.TransitionOrderAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus>(), It.IsAny<OrderStatus>(), It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task CustomPackagingService_CrossTenantSupplier_IsBlockedOnGetAndSubmitRevision()
    {
        var repository = new Mock<ICustomPackagingRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        var shopAId = Guid.NewGuid();
        var supplierBUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        repository.Setup(r => r.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            ShopId = shopAId,
            Status = OrderStatus.Confirmed
        });
        repository.Setup(r => r.IsShopOwnedByUserAsync(shopAId, supplierBUserId, default)).ReturnsAsync(false);

        var service = new CustomPackagingService(repository.Object, clock.Object);

        var getEx = await Assert.ThrowsAsync<HttpException>(() => service.GetAsync(supplierBUserId, true, orderId));
        Assert.Equal(404, getEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, getEx.ErrorCode);

        var submitEx = await Assert.ThrowsAsync<HttpException>(() => service.SubmitRevisionAsync(supplierBUserId, orderId,
            new SubmitPackagingRevisionRequest { MockupUrl = "https://example.com/mockup.png", DesignFee = 100000m }));
        Assert.Equal(404, submitEx.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, submitEx.ErrorCode);
    }

    [Fact]
    public async Task DeliveryService_EnforcesOrderOwnership_AndSingleActiveDeliveryPerOrder()
    {
        var repository = new Mock<IDeliveryRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        var orderId = Guid.NewGuid();
        var supplierAUserId = Guid.NewGuid();
        var supplierBUserId = Guid.NewGuid();
        var customerBUserId = Guid.NewGuid();

        repository.Setup(r => r.CanAccessOrderAsync(orderId, customerBUserId, default)).ReturnsAsync(false);
        repository.Setup(r => r.CanManageOrderAsync(orderId, supplierBUserId, default)).ReturnsAsync(false);
        repository.Setup(r => r.CanManageOrderAsync(orderId, supplierAUserId, default)).ReturnsAsync(true);

        var service = new DeliveryService(repository.Object, clock.Object);

        var forbiddenRead = await Assert.ThrowsAsync<HttpException>(() => service.GetByOrderAsync(customerBUserId, orderId));
        Assert.Equal(403, forbiddenRead.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, forbiddenRead.ErrorCode);

        var deliveryRequest = new CreateDeliveryRequest
        {
            OrderId = orderId,
            FromAddress = "Shop A Factory",
            ToAddress = "Customer A HQ",
            RecipientName = "Customer A",
            RecipientPhone = "0988000101"
        };

        var forbiddenCreate = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(supplierBUserId, deliveryRequest));
        Assert.Equal(403, forbiddenCreate.StatusCode);
        Assert.Equal(OrderErrorCodes.TenantForbidden, forbiddenCreate.ErrorCode);

        repository.Setup(r => r.GetByOrderIdAsync(orderId, default)).ReturnsAsync([
            new Delivery { Id = Guid.NewGuid(), OrderId = orderId, Status = DeliveryStatus.InTransit }
        ]);

        var duplicateCreate = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(supplierAUserId, deliveryRequest));
        Assert.Equal(409, duplicateCreate.StatusCode);
        Assert.Equal(OrderErrorCodes.SingleDeliveryLimit, duplicateCreate.ErrorCode);
    }

    [Fact]
    public async Task OrderService_CreateAsync_PersistsPackagingAtomically_AndBlocksUnsupportedProduct()
    {
        var repository = new Mock<IOrderRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);

        var customerId = Guid.NewGuid();
        var shop = new Shop { Id = Guid.NewGuid(), Status = ShopStatus.Active };
        var standardProduct = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            Name = "Standard Box",
            BasePrice = 200000m,
            MinOrderQty = 10,
            SupportsCustomPackaging = false,
            Status = ProductStatus.Active
        };
        var standardVariant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = standardProduct.Id,
            ShopId = shop.Id,
            Name = "4 Cakes",
            Sku = "STD-4",
            StockQty = 200
        };

        repository.Setup(r => r.GetShopAsync(shop.Id, default)).ReturnsAsync(shop);
        repository.Setup(r => r.GetProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default)).ReturnsAsync([standardProduct]);
        repository.Setup(r => r.GetVariantsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), default)).ReturnsAsync([standardVariant]);
        repository.Setup(r => r.GetActivePromotionsAsync(shop.Id, default)).ReturnsAsync([]);

        var service = new OrderService(repository.Object, clock.Object);
        var packagingRequest = new CreateCustomPackagingRequest
        {
            CompanyName = "ACME Corp",
            CompanyLogoUrl = "https://cdn.example.com/acme-logo.png",
            PackagingType = "Royal Lacquer",
            ColorScheme = "{\"primary\":\"#8c1524\"}"
        };

        var unsupportedEx = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, "pkg-unsupported-01",
            new CreateOrderRequest
            {
                ShopId = shop.Id,
                Items = [new CreateOrderItemRequest { ProductId = standardProduct.Id, VariantId = standardVariant.Id, Quantity = 20 }],
                DeliveryAddress = "10 Nguyen Hue, HCMC",
                RecipientName = "Buyer A",
                RecipientPhone = "0988000101",
                CustomPackaging = packagingRequest
            }));
        Assert.Equal(422, unsupportedEx.StatusCode);
        Assert.Equal(OrderErrorCodes.PackagingNotSupported, unsupportedEx.ErrorCode);
        repository.Verify(r => r.CreateOrderWithPackagingAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), It.IsAny<CustomPackaging>(), default), Times.Never);

        standardProduct.SupportsCustomPackaging = true;
        CustomPackaging? capturedPackaging = null;
        repository.Setup(r => r.CreateOrderWithPackagingAsync(It.IsAny<Order>(), It.IsAny<IReadOnlyList<OrderItem>>(), It.IsAny<CustomPackaging>(), default))
            .Callback<Order, IReadOnlyList<OrderItem>, CustomPackaging, CancellationToken>((_, _, pkg, _) => capturedPackaging = pkg)
            .ReturnsAsync(true);

        var created = await service.CreateAsync(customerId, "pkg-supported-01", new CreateOrderRequest
        {
            ShopId = shop.Id,
            Items = [new CreateOrderItemRequest { ProductId = standardProduct.Id, VariantId = standardVariant.Id, Quantity = 20 }],
            DeliveryAddress = "10 Nguyen Hue, HCMC",
            RecipientName = "Buyer A",
            RecipientPhone = "0988000101",
            CustomPackaging = packagingRequest
        });

        Assert.NotNull(capturedPackaging);
        Assert.NotNull(created.CustomPackaging);
        Assert.Equal("ACME Corp", created.CustomPackaging.CompanyName);
        Assert.Equal(CustomPackagingStatus.Pending, created.CustomPackaging.Status);
    }

    [Fact]
    public void VnPayGateway_UsesOrderDescription_WhenPaymentBelongsToOrder()
    {
        var gateway = new VnPayGateway(Options.Create(new VnPayOptions
        {
            TmnCode = "TESTCODE",
            HashSecret = "test-secret",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        }));
        var orderPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            PaymentNumber = "PAY-20261104-ORD01",
            Amount = 5000000m,
            Currency = "VND",
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        var url = gateway.CreatePaymentUrl(orderPayment, "http://localhost:5173/#/thanh-toan/ket-qua", "127.0.0.1", fixedNow);
        var query = QueryHelpers.ParseQuery(new Uri(url).Query)
            .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);

        Assert.Equal("Order payment PAY-20261104-ORD01", query["vnp_OrderInfo"]);
        Assert.True(gateway.IsValidSignature(query));
    }
}

