namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;

public sealed partial class ShopPostgresTests
{
    [PostgresFact]
    public async Task UploadReadyReplayDoesNotRepeatProviderWorkOrAudits()
    {
        var (owner, _) = await SeedAsync(); var key = Guid.NewGuid().ToString();
        var media = new Mock<IShopMediaGateway>();
        media.Setup(x => x.PrepareAsync(It.IsAny<byte[]>(), "Logo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopProcessedMediaDto([1, 2, 3], "branding.png", "image/png", 128, 128));
        media.Setup(x => x.UploadAsync(It.IsAny<ShopProcessedMediaDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopUploadedMediaDto("https://example.test/logo.png", "images/upload-replay"));
        await using var db = fixture.Create(); var service = Service(db, media.Object);
        using var first = new MemoryStream([1, 2, 3]); var ready = await service.UploadAsync(owner, false, key, "Logo", first, default);
        using var retry = new MemoryStream([1, 2, 3]); var replay = await service.UploadAsync(owner, false, key, "Logo", retry, default);
        Assert.Equal(ready.MediaAssetId, replay.MediaAssetId); Assert.Equal("Ready", replay.Status);
        media.Verify(x => x.UploadAsync(It.IsAny<ShopProcessedMediaDto>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, await db.AuditLogs.CountAsync(x => x.ActorUserId == owner && x.EntityName == "ShopManagement"));
    }

    [PostgresFact]
    public async Task UploadFinalizeRechecksOwnerAfterExternalProviderWork()
    {
        var (owner, _) = await SeedAsync(); var media = new Mock<IShopMediaGateway>();
        media.Setup(x => x.PrepareAsync(It.IsAny<byte[]>(), "Logo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopProcessedMediaDto([1, 2, 3], "branding.png", "image/png", 128, 128));
        media.Setup(x => x.UploadAsync(It.IsAny<ShopProcessedMediaDto>(), It.IsAny<CancellationToken>()))
            .Returns(async () => {
                await using var disable = fixture.Create();
                await disable.Users.Where(x => x.Id == owner).ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));
                return new ShopUploadedMediaDto("https://example.test/disabled.png", "images/disabled-upload");
            });
        media.Setup(x => x.DeleteAsync("images/disabled-upload", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using var db = fixture.Create(); using var bytes = new MemoryStream([1, 2, 3]);
        var failure = await Assert.ThrowsAsync<HttpException>(() => Service(db, media.Object).UploadAsync(owner, false, Guid.NewGuid().ToString(), "Logo", bytes, default));
        Assert.Equal(403, failure.StatusCode);
        Assert.Equal("Failed", (await db.ShopMediaAssets.AsNoTracking().SingleAsync(x => x.OwnerUserId == owner)).Status);
        media.Verify(x => x.DeleteAsync("images/disabled-upload", It.IsAny<CancellationToken>()), Times.Once);
    }

    [PostgresFact]
    public async Task CleanupRetainsProviderProofAndRetriesExpiredLease()
    {
        var (owner, _) = await SeedAsync(); var now = DateTime.UtcNow; var id = Guid.NewGuid();
        await using var db = fixture.Create();
        db.ShopMediaAssets.Add(new ShopMediaAsset { Id = id, OwnerUserId = owner, UploadOperationId = Guid.NewGuid(),
            Purpose = "Logo", RequestHash = new string('A', 64), ProviderPublicId = "images/cleanup-retry",
            PublicUrl = "https://example.test/retry.png", Status = "Ready", CreatedAt = now.AddHours(-25), UpdatedAt = now.AddHours(-25) });
        await db.SaveChangesAsync();
        var media = new Mock<IShopMediaGateway>(); media.Setup(x => x.DeleteAsync("images/cleanup-retry", It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => now);
        var repository = new ShopRepository(db);
        Assert.Equal(0, await ShopMediaCleanupHelper.RunAsync(repository, media.Object, clock.Object, default));
        var pending = await db.ShopMediaAssets.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("Deleting", pending.Status); Assert.False(pending.IsDeleted); Assert.Equal("images/cleanup-retry", pending.ProviderPublicId);
        now = now.AddMinutes(6); media.Setup(x => x.DeleteAsync("images/cleanup-retry", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Assert.Equal(1, await ShopMediaCleanupHelper.RunAsync(repository, media.Object, clock.Object, default));
        Assert.True((await db.ShopMediaAssets.AsNoTracking().SingleAsync(x => x.Id == id)).IsDeleted);
    }
}
