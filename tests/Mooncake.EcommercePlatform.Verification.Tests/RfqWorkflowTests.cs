namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class RfqWorkflowTests
{
    private readonly Mock<IRfqRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid supplierUserId = Guid.NewGuid();
    private readonly Guid supplierProfileId = Guid.NewGuid();
    private readonly DateTime fixedNow = new(2026, 10, 29, 8, 0, 0, DateTimeKind.Utc);

    public RfqWorkflowTests()
    {
        clock.SetupGet(c => c.UtcNow).Returns(fixedNow);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenItemsListIsEmpty()
    {
        var service = new RfqService(repository.Object, clock.Object);
        var request = new CreateRfqRequest
        {
            Title = "Mid-Autumn Corporate Gift",
            DeliveryAddress = "123 Le Loi, District 1, HCMC",
            DeliveryDateRequired = DateOnly.FromDateTime(fixedNow.AddDays(20)),
            BidDeadline = DateTime.SpecifyKind(fixedNow.AddDays(5), DateTimeKind.Utc),
            Items = []
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ThrowsBadRequest_WhenBidDeadlineIsInThePast()
    {
        var service = new RfqService(repository.Object, clock.Object);
        var request = new CreateRfqRequest
        {
            Title = "Mid-Autumn Corporate Gift",
            DeliveryAddress = "123 Le Loi, District 1, HCMC",
            DeliveryDateRequired = DateOnly.FromDateTime(fixedNow.AddDays(20)),
            BidDeadline = DateTime.SpecifyKind(fixedNow.AddHours(-1), DateTimeKind.Utc),
            Items =
            [
                new CreateRfqItemRequest
                {
                    ProductName = "Premium Lava Mooncake",
                    Quantity = 200,
                    Unit = "Box",
                    TargetPrice = 450000m
                }
            ]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(customerId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task PublishAsync_PublishesDraftRfq_WhenValid()
    {
        var rfqId = Guid.NewGuid();
        var rfq = new RequestForQuotation
        {
            Id = rfqId,
            CustomerId = customerId,
            RfqNumber = "RFQ-2026-TEST01",
            Title = "Corporate Mooncake",
            DeliveryAddress = "123 Le Loi, D1, HCMC",
            DeliveryDateRequired = DateOnly.FromDateTime(fixedNow.AddDays(15)),
            BidDeadline = fixedNow.AddDays(5),
            Status = RfqStatus.Draft
        };

        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(rfq);
        repository.Setup(r => r.GetRfqItemsAsync(rfqId, default)).ReturnsAsync(
        [
            new RfqItem { Id = Guid.NewGuid(), RfqId = rfqId, ProductName = "Lava Mooncake", Quantity = 100, Unit = "Box" }
        ]);
        repository.Setup(r => r.GetInvitationsAsync(rfqId, default)).ReturnsAsync([]);
        repository.Setup(r => r.GetBidsAsync(rfqId, null, default)).ReturnsAsync([]);

        var service = new RfqService(repository.Object, clock.Object);
        var response = await service.PublishAsync(customerId, rfqId);

        Assert.Equal(RfqStatus.Open, response.Status);
        repository.Verify(r => r.SaveRfqAsync(It.Is<RequestForQuotation>(x => x.Id == rfqId && x.Status == RfqStatus.Open), default), Times.Once);
    }

    [Fact]
    public async Task SubmitBidAsync_Rejects_WhenRfqIsNotOpen()
    {
        var rfqId = Guid.NewGuid();
        var closedRfq = new RequestForQuotation
        {
            Id = rfqId,
            CustomerId = customerId,
            Status = RfqStatus.Closed,
            BidDeadline = fixedNow.AddDays(-1)
        };

        repository.Setup(r => r.GetVerifiedSupplierProfileIdAsync(supplierUserId, default)).ReturnsAsync(supplierProfileId);
        repository.Setup(r => r.IsActiveSupplierAsync(supplierProfileId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(closedRfq);

        var service = new RfqService(repository.Object, clock.Object);
        var request = new SubmitBidRequest
        {
            EstimatedDeliveryDays = 7,
            ValidityDays = 30,
            Items = [new SubmitBidItemRequest { RfqItemId = Guid.NewGuid(), UnitPrice = 350000m }]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.SubmitBidAsync(supplierUserId, rfqId, request));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitBidAsync_Rejects_WhenItemPriceIsZeroOrNegative()
    {
        var rfqId = Guid.NewGuid();
        var openRfq = new RequestForQuotation
        {
            Id = rfqId,
            CustomerId = customerId,
            Status = RfqStatus.Open,
            BidDeadline = fixedNow.AddDays(3)
        };
        var itemId = Guid.NewGuid();

        repository.Setup(r => r.GetVerifiedSupplierProfileIdAsync(supplierUserId, default)).ReturnsAsync(supplierProfileId);
        repository.Setup(r => r.IsActiveSupplierAsync(supplierProfileId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(openRfq);
        repository.Setup(r => r.GetInvitationAsync(rfqId, supplierProfileId, default))
            .ReturnsAsync(new RfqInvitation { RfqId = rfqId, SupplierId = supplierProfileId, Status = RfqInvitationStatus.Invited });
        repository.Setup(r => r.GetRfqItemsAsync(rfqId, default))
            .ReturnsAsync([new RfqItem { Id = itemId, RfqId = rfqId, ProductName = "Mooncake", Quantity = 100, Unit = "Box" }]);

        var service = new RfqService(repository.Object, clock.Object);
        var request = new SubmitBidRequest
        {
            EstimatedDeliveryDays = 7,
            ValidityDays = 30,
            Items = [new SubmitBidItemRequest { RfqItemId = itemId, UnitPrice = 0m }]
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.SubmitBidAsync(supplierUserId, rfqId, request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AwardBidAsync_Rejects_WhenUserIsNotRfqCustomer()
    {
        var rfqId = Guid.NewGuid();
        var bidId = Guid.NewGuid();
        var someoneElseId = Guid.NewGuid();
        var rfq = new RequestForQuotation
        {
            Id = rfqId,
            CustomerId = someoneElseId,
            Status = RfqStatus.Open
        };

        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(rfq);

        var service = new RfqService(repository.Object, clock.Object);
        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.AwardBidAsync(customerId, rfqId, bidId, new AwardBidRequest { SelectionNote = "Best offer" }));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task AwardBidAsync_Rejects_WhenBidHasExpired()
    {
        var rfqId = Guid.NewGuid();
        var bidId = Guid.NewGuid();
        var rfq = new RequestForQuotation
        {
            Id = rfqId,
            CustomerId = customerId,
            Status = RfqStatus.Closed,
            BidDeadline = fixedNow.AddDays(-2)
        };
        var expiredBid = new Bid
        {
            Id = bidId,
            RfqId = rfqId,
            SupplierId = supplierProfileId,
            Status = BidStatus.Submitted,
            SubmittedAt = fixedNow.AddDays(-40),
            ValidityDays = 30
        };

        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(rfq);
        repository.Setup(r => r.GetInvitationsAsync(rfqId, default)).ReturnsAsync([]);
        repository.Setup(r => r.GetBidAsync(bidId, default)).ReturnsAsync(expiredBid);

        var service = new RfqService(repository.Object, clock.Object);
        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            service.AwardBidAsync(customerId, rfqId, bidId, new AwardBidRequest { SelectionNote = "Best offer" }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task SubmitBidAsync_CreatesBidWithServerQuantityAndCalculatedSubtotal()
    {
        var rfqId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        Bid? savedBid = null;
        IReadOnlyList<BidItem>? savedItems = null;
        repository.Setup(r => r.GetVerifiedSupplierProfileIdAsync(supplierUserId, default)).ReturnsAsync(supplierProfileId);
        repository.Setup(r => r.IsActiveSupplierAsync(supplierProfileId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(new RequestForQuotation
        {
            Id = rfqId, CustomerId = customerId, Status = RfqStatus.Open, BidDeadline = fixedNow.AddDays(2)
        });
        repository.Setup(r => r.GetInvitationAsync(rfqId, supplierProfileId, default)).ReturnsAsync(new RfqInvitation
        {
            RfqId = rfqId, SupplierId = supplierProfileId, Status = RfqInvitationStatus.Invited
        });
        repository.Setup(r => r.GetRfqItemsAsync(rfqId, default)).ReturnsAsync([
            new RfqItem { Id = itemId, RfqId = rfqId, ProductName = "Mooncake box", Quantity = 12, Unit = "Box" }
        ]);
        repository.Setup(r => r.GetBidsAsync(rfqId, supplierProfileId, default)).ReturnsAsync([]);
        repository.Setup(r => r.GetBidItemsAsync(It.IsAny<Guid>(), default)).ReturnsAsync([]);
        repository.Setup(r => r.CreateBidAsync(It.IsAny<Bid>(), It.IsAny<IReadOnlyList<BidItem>>(), default))
            .Callback<Bid, IReadOnlyList<BidItem>, CancellationToken>((bid, items, _) =>
            {
                savedBid = bid;
                savedItems = items;
            }).ReturnsAsync(true);

        var service = new RfqService(repository.Object, clock.Object);
        var response = await service.SubmitBidAsync(supplierUserId, rfqId, new SubmitBidRequest
        {
            EstimatedDeliveryDays = 7,
            ValidityDays = 30,
            Items = [new SubmitBidItemRequest { RfqItemId = itemId, UnitPrice = 125000m }]
        });

        Assert.NotNull(savedBid);
        Assert.Equal(BidStatus.Submitted, savedBid.Status);
        Assert.Equal(1500000m, savedBid.TotalPrice);
        Assert.Equal(1500000m, response.TotalPrice);
        var savedItem = Assert.Single(savedItems!);
        Assert.Equal(12, savedItem.Quantity);
        Assert.Equal(1500000m, savedItem.Subtotal);
        repository.Verify(r => r.CreateBidAsync(It.IsAny<Bid>(), It.IsAny<IReadOnlyList<BidItem>>(), default), Times.Once);
    }

    [Fact]
    public async Task SubmitBidAsync_RejectsBidThatDoesNotPriceEveryRfqItem()
    {
        var rfqId = Guid.NewGuid();
        var firstItemId = Guid.NewGuid();
        repository.Setup(r => r.GetVerifiedSupplierProfileIdAsync(supplierUserId, default)).ReturnsAsync(supplierProfileId);
        repository.Setup(r => r.IsActiveSupplierAsync(supplierProfileId, default)).ReturnsAsync(true);
        repository.Setup(r => r.GetRfqAsync(rfqId, default)).ReturnsAsync(new RequestForQuotation
        {
            Id = rfqId, CustomerId = customerId, Status = RfqStatus.Open, BidDeadline = fixedNow.AddDays(2)
        });
        repository.Setup(r => r.GetInvitationAsync(rfqId, supplierProfileId, default)).ReturnsAsync(new RfqInvitation
        {
            RfqId = rfqId, SupplierId = supplierProfileId, Status = RfqInvitationStatus.Invited
        });
        repository.Setup(r => r.GetRfqItemsAsync(rfqId, default)).ReturnsAsync([
            new RfqItem { Id = firstItemId, RfqId = rfqId, ProductName = "Mooncake box", Quantity = 12, Unit = "Box" },
            new RfqItem { Id = Guid.NewGuid(), RfqId = rfqId, ProductName = "Tea set", Quantity = 5, Unit = "Set" }
        ]);

        var service = new RfqService(repository.Object, clock.Object);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.SubmitBidAsync(supplierUserId, rfqId,
            new SubmitBidRequest
            {
                EstimatedDeliveryDays = 7,
                Items = [new SubmitBidItemRequest { RfqItemId = firstItemId, UnitPrice = 125000m }]
            }));

        Assert.Equal(400, error.StatusCode);
        repository.Verify(r => r.CreateBidAsync(It.IsAny<Bid>(), It.IsAny<IReadOnlyList<BidItem>>(), default), Times.Never);
    }
}

