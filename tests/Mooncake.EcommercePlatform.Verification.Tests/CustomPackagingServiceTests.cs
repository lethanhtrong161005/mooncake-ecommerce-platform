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
}
