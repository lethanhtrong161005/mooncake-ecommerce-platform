namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Products.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Persistence;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Wishlist.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Xunit;

public sealed class WishlistServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private static WishlistItemResponse SampleItem(Guid productId, DateTime savedAtUtc, string name = "Bánh Trung thu Sen") =>
        new(
            Guid.NewGuid(),
            productId,
            name,
            "https://example.com/mooncake.jpg",
            450000m,
            450000m,
            680000m,
            20,
            "hộp",
            true,
            ProductStatus.Active,
            new ProductShopSummaryResponse(Guid.NewGuid(), "xuan-huong", "Xưởng Xuân Hương", 95m, true, "HN"),
            new ProductCategorySummaryResponse(Guid.NewGuid(), "Bánh nướng"),
            4.9m,
            18,
            savedAtUtc);

    private static bool TryValidateDto(object dto, out List<ValidationResult> errors)
    {
        errors = [];
        return Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
    }

    [Fact]
    public async Task GetPageAsync_ProtectsNextCursor_AndRejectsCrossUserOrExpiredCursor()
    {
        var now = new DateTime(2026, 11, 12, 5, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(() => now);
        var protector = new WishlistCursorProtector(new EphemeralDataProtectionProvider(), clock.Object);

        var p1 = Guid.NewGuid();
        var cursorOut = new WishlistCursorDto(UserId, Guid.NewGuid(), now.AddMinutes(-5));
        var repo = new Mock<IWishlistRepository>();
        repo.Setup(x => x.GetPageAsync(UserId, null, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistPageResultDto([SampleItem(p1, now.AddMinutes(-5))], true, cursorOut, 3));

        var service = new WishlistService(repo.Object, protector, clock.Object);

        var page1 = await service.GetPageAsync(UserId, new WishlistPageQuery { PageSize = 2 });

        Assert.True(page1.HasMore);
        Assert.Equal(3, page1.TotalCount);
        Assert.False(string.IsNullOrWhiteSpace(page1.NextCursor));
        Assert.Single(page1.Items);

        repo.Setup(x => x.GetPageAsync(UserId, cursorOut, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WishlistPageResultDto([], false, null, 3));

        var page2 = await service.GetPageAsync(UserId, new WishlistPageQuery { PageSize = 2, Cursor = page1.NextCursor });
        Assert.False(page2.HasMore);
        Assert.Null(page2.NextCursor);

        var tampered = await Assert.ThrowsAsync<HttpException>(() =>
            service.GetPageAsync(OtherUserId, new WishlistPageQuery { PageSize = 2, Cursor = page1.NextCursor }));
        Assert.Equal(400, tampered.StatusCode);

        now = now.AddMinutes(20);
        var expired = await Assert.ThrowsAsync<HttpException>(() =>
            service.GetPageAsync(UserId, new WishlistPageQuery { PageSize = 2, Cursor = page1.NextCursor }));
        Assert.Equal(400, expired.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(51)]
    public void WishlistPageQuery_RejectsInvalidPageSize_AtDtoLayer(int pageSize)
    {
        var dto = new WishlistPageQuery { PageSize = pageSize };
        Assert.False(TryValidateDto(dto, out _));
    }

    [Fact]
    public void WishlistPageQuery_RejectsWhitespaceCursor_AtDtoLayer()
    {
        Assert.True(TryValidateDto(new WishlistPageQuery(), out _));
        Assert.False(TryValidateDto(new WishlistPageQuery { Cursor = "   " }, out _));
        Assert.False(TryValidateDto(new WishlistPageQuery { Cursor = "" }, out _));
    }

    [Fact]
    public void NonEmptyGuidAttribute_RejectsEmptyAndNull()
    {
        var attr = new NonEmptyGuidAttribute();
        Assert.False(attr.IsValid(null));
        Assert.False(attr.IsValid(Guid.Empty));
        Assert.True(attr.IsValid(Guid.NewGuid()));
    }

    [Fact]
    public void WishlistCheckQuery_ValidatesNullEmptyMalformedEmptyGuidAndOverLimit_AtDtoLayer()
    {
        Assert.False(TryValidateDto(new WishlistCheckQuery(), out _));
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = [] }, out _));
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = ["  ", ""] }, out _));
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = [",,,"] }, out _));
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = ["not-a-guid"] }, out _));
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = [Guid.Empty.ToString()] }, out _));

        var overLimit = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid().ToString()).ToArray();
        Assert.False(TryValidateDto(new WishlistCheckQuery { Ids = overLimit }, out _));

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var valid = new WishlistCheckQuery { Ids = [$"{id1},{id1}", id2.ToString()] };
        Assert.True(TryValidateDto(valid, out _));
        Assert.Equal([id1, id2], valid.ToDistinctProductIds());
    }

    [Fact]
    public async Task ToggleAsync_DelegatesAddAndRemoveStates()
    {
        var now = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        var repo = new Mock<IWishlistRepository>();
        repo.SetupSequence(x => x.ToggleAsync(UserId, productId, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToggleWishlistResponse(productId, true, 1))
            .ReturnsAsync(new ToggleWishlistResponse(productId, false, 0));

        var service = new WishlistService(repo.Object, Mock.Of<IWishlistCursorProtector>(), clock.Object);

        var added = await service.ToggleAsync(UserId, productId);
        Assert.True(added.IsSaved);
        Assert.Equal(1, added.TotalCount);

        var removed = await service.ToggleAsync(UserId, productId);
        Assert.False(removed.IsSaved);
        Assert.Equal(0, removed.TotalCount);
    }

    [Fact]
    public async Task CheckBatchAsync_PassesDistinctDtoIdsToRepository()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var repo = new Mock<IWishlistRepository>();
        repo.Setup(x => x.GetSavedProductIdsAsync(
                UserId,
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([id1]);

        var service = new WishlistService(repo.Object, Mock.Of<IWishlistCursorProtector>(), Mock.Of<IDateTimeProvider>());

        var checkedResult = await service.CheckBatchAsync(
            UserId,
            new WishlistCheckQuery { Ids = [$"{id1},{id1}", id2.ToString()] });

        Assert.Single(checkedResult.SavedIds);
        Assert.Equal(id1, checkedResult.SavedIds[0]);
    }
}
