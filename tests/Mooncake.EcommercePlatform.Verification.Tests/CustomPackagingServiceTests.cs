namespace Mooncake.EcommercePlatform.Verification.Tests;

using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Xunit;

public sealed class CustomPackagingServiceTests
{
    private readonly Mock<ICustomPackagingRepository> repository = new();
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid orderId = Guid.NewGuid();

    [Fact]
    public async Task CreateNormalizesRequestAndPersistsPendingPackaging()
    {
        var now = new DateTime(2026, 10, 29, 4, 30, 0, DateTimeKind.Utc);
        repository.Setup(item => item.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId, CustomerId = customerId, ShopId = Guid.NewGuid(), Status = OrderStatus.Pending
        });
        repository.Setup(item => item.GetByOrderIdAsync(orderId, default))
            .ReturnsAsync((CustomPackaging?)null);
        repository.Setup(item => item.CreateAsync(It.IsAny<CustomPackaging>(), customerId, default))
            .ReturnsAsync(true);
        clock.SetupGet(item => item.UtcNow).Returns(now);
        var service = new CustomPackagingService(repository.Object, clock.Object);

        var response = await service.CreateAsync(customerId, orderId, new CreateCustomPackagingRequest
        {
            CompanyName = "  Mooncake Corp  ",
            CompanyLogoUrl = "https://cdn.example.com/logo.svg",
            PackagingType = "  Premium box  ",
            ColorScheme = "{\"primary\":\"#8c1524\"}"
        });

        Assert.Equal(CustomPackagingStatus.Pending, response.Status);
        Assert.Equal("Mooncake Corp", response.CompanyName);
        Assert.Equal("Premium box", response.PackagingType);
        repository.Verify(item => item.CreateAsync(It.Is<CustomPackaging>(packaging =>
            packaging.CreatedAt == now && packaging.CompanyName == "Mooncake Corp"),
            customerId, default), Times.Once);
    }

    [Fact]
    public async Task ReviewRevisionRequiresUsefulNotesWhenRequestingChanges()
    {
        var packaging = new CustomPackaging { Id = Guid.NewGuid(), OrderId = orderId };
        var revision = new CustomPackagingRevision
        {
            Id = Guid.NewGuid(), CustomPackagingId = packaging.Id,
            Status = CustomPackagingStatus.DesignReview
        };
        repository.Setup(item => item.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId, CustomerId = customerId, ShopId = Guid.NewGuid()
        });
        repository.Setup(item => item.GetByOrderIdAsync(orderId, default)).ReturnsAsync(packaging);
        repository.Setup(item => item.GetRevisionAsync(revision.Id, default)).ReturnsAsync(revision);
        var service = new CustomPackagingService(repository.Object, clock.Object);

        var failure = await Assert.ThrowsAsync<HttpException>(() => service.ReviewRevisionAsync(
            customerId, orderId, revision.Id,
            new ReviewPackagingRevisionRequest { Action = "RequestRevision", Notes = "Too short" }));

        Assert.Equal(400, failure.StatusCode);
        repository.Verify(item => item.ReviewRevisionAsync(It.IsAny<CustomPackaging>(),
            It.IsAny<CustomPackagingRevision>(), It.IsAny<bool>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task SubmitRevision_WithSurchargeAtRevision3_Throws400()
    {
        var supplierId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var packaging = new CustomPackaging { Id = Guid.NewGuid(), OrderId = orderId };
        var rev1 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 1 };
        var rev2 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 2 };

        repository.Setup(item => item.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId, CustomerId = customerId, ShopId = shopId
        });
        repository.Setup(item => item.IsShopOwnedByUserAsync(shopId, supplierId, default)).ReturnsAsync(true);
        repository.Setup(item => item.GetByOrderIdAsync(orderId, default)).ReturnsAsync(packaging);
        repository.Setup(item => item.GetRevisionsAsync(packaging.Id, default)).ReturnsAsync([rev1, rev2]);

        var service = new CustomPackagingService(repository.Object, clock.Object);

        var ex = await Assert.ThrowsAsync<HttpException>(() => service.SubmitRevisionAsync(supplierId, orderId, new SubmitPackagingRevisionRequest
        {
            MockupUrl = "https://cdn.example.com/mockup3.png",
            RevisionSurcharge = 50_000m
        }));

        Assert.Equal(400, ex.StatusCode);
        repository.Verify(item => item.SubmitRevisionAsync(It.IsAny<CustomPackaging>(), It.IsAny<CustomPackagingRevision>(), It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task SubmitRevision_WithSurchargeAtRevision5_SucceedsAndResponseContainsLimits()
    {
        var supplierId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var now = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
        clock.SetupGet(item => item.UtcNow).Returns(now);

        var packaging = new CustomPackaging { Id = Guid.NewGuid(), OrderId = orderId };
        var rev1 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 1, DesignFee = 500_000m };
        var rev2 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 2 };
        var rev3 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 3 };
        var rev4 = new CustomPackagingRevision { Id = Guid.NewGuid(), CustomPackagingId = packaging.Id, RevisionNumber = 4 };

        repository.Setup(item => item.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId, CustomerId = customerId, ShopId = shopId
        });
        repository.Setup(item => item.IsShopOwnedByUserAsync(shopId, supplierId, default)).ReturnsAsync(true);
        repository.Setup(item => item.GetByOrderIdAsync(orderId, default)).ReturnsAsync(packaging);

        var rev5 = new CustomPackagingRevision
        {
            Id = Guid.NewGuid(),
            CustomPackagingId = packaging.Id,
            RevisionNumber = 5,
            MockupUrl = "https://cdn.example.com/mockup5.png",
            DesignFee = 0m,
            RevisionSurcharge = 150_000m,
            Status = CustomPackagingStatus.DesignReview,
            SubmittedAt = now
        };

        repository.SetupSequence(item => item.GetRevisionsAsync(packaging.Id, default))
            .ReturnsAsync([rev1, rev2, rev3, rev4])
            .ReturnsAsync([rev1, rev2, rev3, rev4, rev5]);

        repository.Setup(item => item.SubmitRevisionAsync(packaging, It.Is<CustomPackagingRevision>(r => r.RevisionSurcharge == 150_000m), customerId, default))
            .ReturnsAsync(rev5);

        var service = new CustomPackagingService(repository.Object, clock.Object);

        var response = await service.SubmitRevisionAsync(supplierId, orderId, new SubmitPackagingRevisionRequest
        {
            MockupUrl = "https://cdn.example.com/mockup5.png",
            RevisionSurcharge = 150_000m
        });

        Assert.Equal(5, response.Revisions.Count);
        Assert.Equal(3, response.FreeRevisionsUsed);
        Assert.Equal(3, response.FreeRevisionsLimit);
        Assert.Equal(150_000m, response.RevisionSurcharge);
        Assert.Equal(150_000m, response.Revisions[4].RevisionSurcharge);
        repository.Verify(item => item.SubmitRevisionAsync(packaging, It.Is<CustomPackagingRevision>(r => r.RevisionSurcharge == 150_000m), customerId, default), Times.Once);
    }

    [Fact]
    public async Task ReviewRevision_ApproveAtRevision5_SucceedsAndAppliesApproval()
    {
        var now = new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
        clock.SetupGet(item => item.UtcNow).Returns(now);

        var packaging = new CustomPackaging { Id = Guid.NewGuid(), OrderId = orderId };
        var rev5 = new CustomPackagingRevision
        {
            Id = Guid.NewGuid(),
            CustomPackagingId = packaging.Id,
            RevisionNumber = 5,
            MockupUrl = "https://cdn.example.com/mockup5.png",
            DesignFee = 500_000m,
            RevisionSurcharge = 150_000m,
            Status = CustomPackagingStatus.DesignReview
        };

        repository.Setup(item => item.GetOrderAsync(orderId, default)).ReturnsAsync(new Order
        {
            Id = orderId, CustomerId = customerId, ShopId = Guid.NewGuid()
        });
        repository.Setup(item => item.GetByOrderIdAsync(orderId, default)).ReturnsAsync(packaging);
        repository.Setup(item => item.GetRevisionAsync(rev5.Id, default)).ReturnsAsync(rev5);
        repository.Setup(item => item.ReviewRevisionAsync(packaging, rev5, true, null, customerId, now, default))
            .Callback<CustomPackaging, CustomPackagingRevision, bool, string?, Guid, DateTime, CancellationToken>(
                (pkg, rev, approve, notes, actor, dt, ct) =>
                {
                    pkg.Status = CustomPackagingStatus.Approved;
                    pkg.BoxDesignUrl = rev.MockupUrl;
                    rev.Status = CustomPackagingStatus.Approved;
                })
            .ReturnsAsync(true);

        var service = new CustomPackagingService(repository.Object, clock.Object);

        var reviewResponse = await service.ReviewRevisionAsync(customerId, orderId, rev5.Id, new ReviewPackagingRevisionRequest
        {
            Action = "Approve"
        });

        Assert.Equal(CustomPackagingStatus.Approved, reviewResponse.PackagingStatus);
        Assert.Equal("https://cdn.example.com/mockup5.png", reviewResponse.ApprovedMockupUrl);
        repository.Verify(item => item.ReviewRevisionAsync(packaging, rev5, true, null, customerId, now, default), Times.Once);
    }
}
