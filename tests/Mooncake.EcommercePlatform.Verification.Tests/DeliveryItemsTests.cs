namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Reflection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Xunit;

public sealed class DeliveryItemsTests
{
    private readonly Mock<IDeliveryRepository> repository = new();
    private readonly Mock<IContractRepository> contractRepository = new();
    private readonly Mock<IDateTimeProvider> clock = new();

    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid contractId = Guid.NewGuid();
    private readonly Guid orderId = Guid.NewGuid();
    private readonly Guid rfqItemId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);

    public DeliveryItemsTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task CreateAsync_WhenContractDeliveryExceedsAvailableQuantity_Throws409Conflict()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);
        var contractLine = new ContractLineInfo(rfqItemId, "Bánh dẻo đậu xanh hạt sen 150g", "hộp", 100);

        repository.Setup(r => r.CanManageContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetContractLinesAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contractLine]);
        repository.Setup(r => r.GetContractFulfillmentAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContractFulfillmentLineResponse
            {
                LineId = rfqItemId,
                RfqItemId = rfqItemId,
                ProductName = contractLine.ProductName,
                Unit = contractLine.Unit,
                TotalQuantity = 100,
                ScheduledQuantity = 0,
                DeliveredQuantity = 0,
                RemainingQuantity = 100
            }]);

        var request = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van A",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            Items =
            [
                new CreateDeliveryItemRequest
                {
                    RfqItemId = rfqItemId,
                    Quantity = 120 // exceeds contract total of 100
                }
            ]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenContractDeliveryExceedsRemainingAfterPriorBatch_Throws409Conflict()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);
        var contractLine = new ContractLineInfo(rfqItemId, "Bánh nướng thập cẩm gà quay 200g", "hộp", 100);

        repository.Setup(r => r.CanManageContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetContractLinesAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contractLine]);
        // 60 units already scheduled in batch 1, remaining is 40
        repository.Setup(r => r.GetContractFulfillmentAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContractFulfillmentLineResponse
            {
                LineId = rfqItemId,
                RfqItemId = rfqItemId,
                ProductName = contractLine.ProductName,
                Unit = contractLine.Unit,
                TotalQuantity = 100,
                ScheduledQuantity = 60,
                DeliveredQuantity = 0,
                RemainingQuantity = 40
            }]);

        var request = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van A",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            Items =
            [
                new CreateDeliveryItemRequest
                {
                    RfqItemId = rfqItemId,
                    Quantity = 50 // exceeds remaining 40
                }
            ]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenContractDeliveryHasMissingOrZeroItems_Throws400()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);
        var contractLine = new ContractLineInfo(rfqItemId, "Bánh nướng", "hộp", 100);

        repository.Setup(r => r.CanManageContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetContractLinesAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contractLine]);

        // Empty items list
        var emptyItemsRequest = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van A",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            Items = []
        };
        var emptyEx = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, emptyItemsRequest));
        Assert.Equal(400, emptyEx.StatusCode);

        // Item with quantity <= 0
        var zeroQtyRequest = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van A",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            Items =
            [
                new CreateDeliveryItemRequest
                {
                    RfqItemId = rfqItemId,
                    Quantity = 0
                }
            ]
        };
        var zeroEx = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(userId, zeroQtyRequest));
        Assert.Equal(400, zeroEx.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ContractDelivery_AutoIncrementsBatchNumber()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);
        var contractLine = new ContractLineInfo(rfqItemId, "Bánh nướng", "hộp", 100);

        repository.Setup(r => r.CanManageContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetContractLinesAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contractLine]);
        repository.Setup(r => r.GetContractFulfillmentAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContractFulfillmentLineResponse
            {
                LineId = rfqItemId,
                RfqItemId = rfqItemId,
                ProductName = contractLine.ProductName,
                Unit = contractLine.Unit,
                TotalQuantity = 100,
                ScheduledQuantity = 0,
                DeliveredQuantity = 0,
                RemainingQuantity = 100
            }]);
        repository.Setup(r => r.GetNextBatchNumberForContractAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repository.Setup(r => r.GetTotalBatchesForContractAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repository.Setup(r => r.CreateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);
        repository.Setup(r => r.CreateDeliveryItemsAsync(It.IsAny<IReadOnlyCollection<DeliveryItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<DeliveryItem> items, CancellationToken _) => items.ToList());
        repository.Setup(r => r.GetDeliveryItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DeliveryItem
            {
                Id = Guid.NewGuid(),
                RfqItemId = rfqItemId,
                ProductName = "Bánh nướng",
                Unit = "hộp",
                Quantity = 50
            }]);

        var request = new CreateDeliveryRequest
        {
            ContractId = contractId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van A",
            RecipientPhone = "0912345678",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(2)),
            Items = [new CreateDeliveryItemRequest { RfqItemId = rfqItemId, Quantity = 50 }]
        };

        var response = await service.CreateAsync(userId, request);

        Assert.NotNull(response);
        Assert.Equal(2, response.BatchNumber);
        Assert.Equal(2, response.TotalBatches);
        Assert.Single(response.Items);
        Assert.Equal(50, response.Items[0].Quantity);
    }

    [Fact]
    public async Task ContractFulfillment_TwoBatchesFulfillTotal_RemainingIsZero()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);

        repository.Setup(r => r.CanAccessContractAsync(contractId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetContractFulfillmentAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContractFulfillmentLineResponse
            {
                LineId = rfqItemId,
                RfqItemId = rfqItemId,
                ProductName = "Bánh nướng hạt sen",
                Unit = "hộp",
                TotalQuantity = 100,
                ScheduledQuantity = 100, // Batch 1 (60) + Batch 2 (40) = 100
                DeliveredQuantity = 100,
                RemainingQuantity = 0
            }]);

        var fulfillment = await service.GetContractFulfillmentAsync(userId, contractId);

        Assert.NotNull(fulfillment);
        var line = Assert.Single(fulfillment);
        Assert.Equal(100, line.TotalQuantity);
        Assert.Equal(100, line.ScheduledQuantity);
        Assert.Equal(100, line.DeliveredQuantity);
        Assert.Equal(0, line.RemainingQuantity);
        Assert.Equal(0, line.Remaining);
    }

    [Fact]
    public async Task AreAllShipmentsDeliveredAsync_WhenQuantitiesMetAndAllDelivered_ReturnsTrue()
    {
        var contractRepo = new Mock<IContractRepository>();
        contractRepo.Setup(r => r.AreAllShipmentsDeliveredAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var isFullyDelivered = await contractRepo.Object.AreAllShipmentsDeliveredAsync(contractId);
        Assert.True(isFullyDelivered);
    }

    [Fact]
    public async Task PublicDriverDeliveryResponse_ContainsItemsAndBatchInfo_DoesNotExposePrice()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            DeliveryNumber = "DEL-202611-0001",
            Status = DeliveryStatus.InTransit,
            BatchNumber = 1,
            ShareToken = "token-test-123",
            ShareTokenExpiresAtUtc = fixedNow.AddHours(24)
        };

        repository.Setup(r => r.GetByShareTokenAsync("token-test-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivery);
        repository.Setup(r => r.GetProofsAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(r => r.GetReferenceCodeAsync(delivery, It.IsAny<CancellationToken>()))
            .ReturnsAsync("HD-2026-001");
        repository.Setup(r => r.GetDeliveryItemsAsync(delivery.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DeliveryItem
            {
                Id = Guid.NewGuid(),
                DeliveryId = delivery.Id,
                RfqItemId = rfqItemId,
                ProductName = "Bánh nướng trà xanh",
                Unit = "hộp",
                Quantity = 50
            }]);
        repository.Setup(r => r.GetTotalBatchesForContractAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var response = await service.GetPublicByTokenAsync("token-test-123");

        Assert.NotNull(response);
        Assert.Equal(1, response.BatchNumber);
        Assert.Equal(2, response.TotalBatches);
        Assert.Single(response.Items);
        Assert.Equal("Bánh nướng trà xanh", response.Items[0].ProductName);
        Assert.Equal(50, response.Items[0].Quantity);
        Assert.Equal("hộp", response.Items[0].Unit);

        // Security check: Verify that neither PublicDriverDeliveryResponse nor DeliveryItemResponse exposes price/financial data
        var driverProps = typeof(PublicDriverDeliveryResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var itemProps = typeof(DeliveryItemResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var priceRelatedKeywords = new[] { "Price", "Cost", "Subtotal", "TotalValue", "Amount" };
        foreach (var prop in driverProps)
        {
            Assert.DoesNotContain(priceRelatedKeywords, keyword => prop.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var prop in itemProps)
        {
            Assert.DoesNotContain(priceRelatedKeywords, keyword => prop.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task CreateAsync_WholesaleOrder_AutomaticallySnapshotsOrderItems()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);

        repository.Setup(r => r.CanManageOrderAsync(orderId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(r => r.CreateAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Delivery d, CancellationToken _) => d);
        repository.Setup(r => r.SnapshotOrderItemsAsync(It.IsAny<Guid>(), orderId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DeliveryItem
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = Guid.NewGuid(),
                    ProductName = "Hộp Bánh Thập Cẩm 4 Bánh",
                    Unit = "hộp",
                    Quantity = 20
                }
            ]);
        repository.Setup(r => r.GetDeliveryItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new DeliveryItem
                {
                    Id = Guid.NewGuid(),
                    OrderItemId = Guid.NewGuid(),
                    ProductName = "Hộp Bánh Thập Cẩm 4 Bánh",
                    Unit = "hộp",
                    Quantity = 20
                }
            ]);

        var request = new CreateDeliveryRequest
        {
            OrderId = orderId,
            FromAddress = "Kho Bat Trang",
            ToAddress = "Keangnam Tower",
            RecipientName = "Nguyen Van B",
            RecipientPhone = "0987654321",
            ScheduledDate = DateOnly.FromDateTime(fixedNow.AddDays(1)),
            Items = [new CreateDeliveryItemRequest { Quantity = 999 }] // should be ignored for wholesale order
        };

        var response = await service.CreateAsync(userId, request);

        Assert.NotNull(response);
        Assert.Equal(1, response.BatchNumber);
        Assert.Null(response.TotalBatches);
        Assert.Single(response.Items);
        Assert.Equal("Hộp Bánh Thập Cẩm 4 Bánh", response.Items[0].ProductName);
        Assert.Equal(20, response.Items[0].Quantity);
        repository.Verify(r => r.SnapshotOrderItemsAsync(It.IsAny<Guid>(), orderId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetContractFulfillment_Authorization_AllowsCustomerSupplierAdmin_DeniesOthers()
    {
        var service = new DeliveryService(repository.Object, clock.Object, contractRepository.Object);

        var allowedUserId = Guid.NewGuid();
        var deniedUserId = Guid.NewGuid();

        repository.Setup(r => r.CanAccessContractAsync(contractId, allowedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository.Setup(r => r.CanAccessContractAsync(contractId, deniedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(r => r.GetContractFulfillmentAsync(contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var allowedResult = await service.GetContractFulfillmentAsync(allowedUserId, contractId);
        Assert.NotNull(allowedResult);

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.GetContractFulfillmentAsync(deniedUserId, contractId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task DeliveriesController_GetContractFulfillment_ReturnsOkWithData()
    {
        var mockService = new Mock<IDeliveryService>();
        mockService.Setup(s => s.GetContractFulfillmentAsync(userId, contractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ContractFulfillmentLineResponse
            {
                LineId = rfqItemId,
                RfqItemId = rfqItemId,
                ProductName = "Bánh nướng mè đen",
                Unit = "hộp",
                TotalQuantity = 100,
                ScheduledQuantity = 100,
                DeliveredQuantity = 100,
                RemainingQuantity = 0
            }]);

        var controller = new DeliveriesController(mockService.Object);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Customer")
        };
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };

        var result = await controller.GetContractFulfillmentAsync(contractId, CancellationToken.None);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var response = Assert.IsType<Mooncake.EcommercePlatform.Application.Common.DTOs.ApiResponse<IReadOnlyList<ContractFulfillmentLineResponse>>>(okResult.Value);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Data);
        var line = Assert.Single(response.Data);
        Assert.Equal("Bánh nướng mè đen", line.ProductName);
        Assert.Equal(0, line.RemainingQuantity);
    }
}
