namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;

public sealed partial class ShopPostgresTests
{
    [PostgresFact]
    public async Task ConcurrentSuppliersCannotClaimSameSlug()
    {
        var (first, _) = await SeedAsync(); var (second, _) = await SeedAsync(); var request = Request();
        async Task<int> Create(Guid actor) {
            await using var db = fixture.Create();
            try { await Service(db).CreateAsync(actor, Guid.NewGuid().ToString(), request, default); return 201; }
            catch (HttpException failure) { Assert.Equal("SHOP_SLUG_IN_USE", failure.ErrorCode); return failure.StatusCode; }
        }
        Assert.Equal(new[] { 201, 409 }, (await Task.WhenAll(Create(first), Create(second))).Order().ToArray());
        await using var read = fixture.Create();
        Assert.Equal(1, await read.Shops.CountAsync(x => x.Slug == request.Slug));
    }

    [PostgresFact]
    public async Task ConcurrentInformationAndAppearanceRejectLostUpdate()
    {
        var (owner, _) = await SeedAsync();
        await using (var db = fixture.Create()) await Service(db).CreateAsync(owner, Guid.NewGuid().ToString(), Request(), default);
        async Task<int> Information() {
            await using var db = fixture.Create();
            try { await Service(db).UpdateAsync(owner, Guid.NewGuid().ToString(), new() { Name = "New name", ExpectedVersion = 1 }, default); return 200; }
            catch (HttpException failure) { return failure.StatusCode; }
        }
        async Task<int> Appearance() {
            await using var db = fixture.Create();
            try { await Service(db).AppearanceAsync(owner, Guid.NewGuid().ToString(), new() {
                ExpectedVersion = 1, TemplateId = Templates[0], TemplateRevisionId = Revisions[0],
                Overrides = new() { BorderRadiusPx = 20 } }, default); return 200; }
            catch (HttpException failure) { return failure.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Information(), Appearance())).Order().ToArray());
        await using var read = fixture.Create();
        Assert.Equal(2, (await read.Shops.SingleAsync(x => x.OwnerUserId == owner)).Version);
        Assert.Equal(2, await read.AuditLogs.CountAsync(x => x.ActorUserId == owner && x.EntityName == "ShopManagement"));
    }

    [PostgresFact]
    public async Task WaitingWriteRechecksDisabledOwnerAndEveryPublicEndpointCloses()
    {
        var (owner, _) = await SeedAsync(); var request = Request();
        await using (var create = fixture.Create()) {
            var service = Service(create); await service.CreateAsync(owner, Guid.NewGuid().ToString(), request, default);
            await service.VisibilityAsync(owner, Guid.NewGuid().ToString(), new() { ExpectedVersion = 1, Status = ShopStatus.Active }, default);
        }
        await using var blocker = fixture.Create(); await using var transaction = await blocker.Database.BeginTransactionAsync();
        var account = await blocker.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {owner} FOR UPDATE").SingleAsync();
        account.IsActive = false; await blocker.SaveChangesAsync();
        var barrier = new LockAttemptInterceptor();
        async Task<HttpException> WaitingWrite() {
            await using var waiting = fixture.Create(barrier);
            return await Assert.ThrowsAsync<HttpException>(() => Service(waiting).UpdateAsync(owner, Guid.NewGuid().ToString(),
                new() { ExpectedVersion = 2, Name = "Must never save" }, default));
        }
        var pending = WaitingWrite(); await barrier.Attempt.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await transaction.CommitAsync();
        Assert.Equal(403, (await pending).StatusCode);
        await using var read = fixture.Create(); var serviceRead = Service(read);
        Assert.Equal(404, (await Assert.ThrowsAsync<HttpException>(() => serviceRead.PublicAsync(request.Slug, default))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<HttpException>(() => serviceRead.ProductsAsync(request.Slug, new(), default))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<HttpException>(() => serviceRead.CategoriesAsync(request.Slug, new(), default))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<HttpException>(() => serviceRead.ReviewsAsync(request.Slug, new(), default))).StatusCode);
        Assert.Equal(request.Name, (await read.Shops.SingleAsync(x => x.OwnerUserId == owner)).Name);
    }

    [PostgresFact]
    public async Task ReplayedKeyCannotChangePayload()
    {
        var (owner, _) = await SeedAsync(); var key = Guid.NewGuid().ToString(); var request = Request();
        await using var db = fixture.Create(); var service = Service(db);
        await service.CreateAsync(owner, key, request, default);
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(owner, key, request with { Name = "Different payload" }, default));
        Assert.Equal("SHOP_IDEMPOTENCY_PAYLOAD_MISMATCH", error.ErrorCode);
    }

    [PostgresFact]
    public async Task ForeignOwnerMediaIsBlockedByDatabaseAndCleanupRetainsReferencedAsset()
    {
        var (owner, _) = await SeedAsync(); var (foreign, _) = await SeedAsync(); Guid shopId;
        await using (var create = fixture.Create()) shopId = (await Service(create).CreateAsync(owner, Guid.NewGuid().ToString(), Request(), default)).ShopId;
        var old = DateTime.UtcNow.AddHours(-25);
        var owned = new ShopMediaAsset { Id = Guid.NewGuid(), OwnerUserId = owner, UploadOperationId = Guid.NewGuid(),
            Purpose = "Logo", RequestHash = new string('A', 64), Status = "Ready", ProviderPublicId = "images/owned-test",
            PublicUrl = "https://example.test/owned.png", CreatedAt = old, UpdatedAt = old };
        var orphan = new ShopMediaAsset { Id = Guid.NewGuid(), OwnerUserId = foreign, UploadOperationId = Guid.NewGuid(),
            Purpose = "Logo", RequestHash = new string('B', 64), Status = "Ready", ProviderPublicId = "images/orphan-test",
            PublicUrl = "https://example.test/orphan.png", CreatedAt = old, UpdatedAt = old };
        await using (var setup = fixture.Create()) {
            setup.ShopMediaAssets.AddRange(owned, orphan); await setup.SaveChangesAsync();
            var shop = await setup.Shops.SingleAsync(x => x.Id == shopId); shop.LogoAssetId = orphan.Id;
            await Assert.ThrowsAsync<DbUpdateException>(() => setup.SaveChangesAsync());
            shop.LogoAssetId = owned.Id; await setup.SaveChangesAsync();
        }
        var media = new Mock<IShopMediaGateway>(); media.Setup(x => x.DeleteAsync(orphan.ProviderPublicId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);
        await using var cleanup = fixture.Create();
        Assert.Equal(1, await ShopMediaCleanupHelper.RunAsync(new ShopRepository(cleanup), media.Object, clock.Object, default));
        media.Verify(x => x.DeleteAsync(owned.ProviderPublicId, It.IsAny<CancellationToken>()), Times.Never);
        Assert.False((await cleanup.ShopMediaAssets.AsNoTracking().SingleAsync(x => x.Id == owned.Id)).IsDeleted);
        Assert.True((await cleanup.ShopMediaAssets.AsNoTracking().SingleAsync(x => x.Id == orphan.Id)).IsDeleted);
    }

    [PostgresFact]
    public async Task AdminQueryCountDoesNotGrowWithPageSize()
    {
        var (_, admin) = await SeedAsync(); var counter = new ShopQueryCounter();
        await using var db = fixture.Create(counter); var service = Service(db);
        await service.ListAdminAsync(admin, new() { PageSize = 10 }, default); var small = counter.Reads;
        counter.Reads = 0; await service.ListAdminAsync(admin, new() { PageSize = 100 }, default);
        Assert.Equal(small, counter.Reads); Assert.InRange(small, 2, 5);
    }

    [PostgresFact]
    public async Task PublicAndPreviewQueriesStayWithinBudget()
    {
        var (owner, _) = await SeedAsync(); var request = Request();
        await using (var create = fixture.Create()) {
            await Service(create).CreateAsync(owner, Guid.NewGuid().ToString(), request, default);
            await Service(create).VisibilityAsync(owner, Guid.NewGuid().ToString(), new() { ExpectedVersion = 1, Status = ShopStatus.Active }, default);
        }
        var counter = new ShopQueryCounter(); await using var read = fixture.Create(counter); var service = Service(read);
        await service.PublicAsync(request.Slug, default); Assert.Equal(2, counter.Reads);
        counter.Reads = 0; await service.ProductsAsync(request.Slug, new(), default); Assert.Equal(2, counter.Reads);
        counter.Reads = 0; await service.PreviewAsync(owner, default); Assert.Equal(3, counter.Reads);
    }

    [PostgresFact]
    public async Task HiddenTemplateKeepsPinnedRevisionAfterPublishing()
    {
        var (owner, admin) = await SeedAsync();
        await using var db = fixture.Create(); await db.ShopTemplates.ExecuteUpdateAsync(x => x.SetProperty(t => t.IsActive, true));
        var service = Service(db);
        var shop = await service.CreateAsync(owner, Guid.NewGuid().ToString(), Request() with { TemplateId = Templates[1], TemplateRevisionId = Revisions[1] }, default);
        var template = await service.TemplateDetailAsync(admin, Templates[1], default);
        var published = await service.PublishTemplateAsync(admin, template.Id, Guid.NewGuid().ToString(), new() {
            ExpectedVersion = template.Version, Name = "New immutable revision", RendererKey = "royal-gift", SchemaVersion = 1,
            SortOrder = template.SortOrder, Defaults = template.CurrentRevision!.Defaults, Layout = template.CurrentRevision.Layout }, default);
        await service.TemplateAvailabilityAsync(admin, template.Id, Guid.NewGuid().ToString(), new() {
            ExpectedVersion = published.Version, IsActive = false }, default);
        var saved = await service.AppearanceAsync(owner, Guid.NewGuid().ToString(), new() {
            ExpectedVersion = shop.Version, TemplateId = template.Id, TemplateRevisionId = Revisions[1], Overrides = new() { BorderRadiusPx = 18 } }, default);
        Assert.Equal(Revisions[1], saved.Appearance.TemplateRevisionId);
        Assert.Equal("heritage", saved.Appearance.RendererKey);
        Assert.False(saved.Appearance.IsSelectable);
        Assert.Equal(18, saved.Appearance.EffectiveTokens.BorderRadiusPx);
        var (another, _) = await SeedAsync();
        var error = await Assert.ThrowsAsync<HttpException>(() => service.CreateAsync(another, Guid.NewGuid().ToString(), Request() with {
            TemplateId = template.Id, TemplateRevisionId = published.CurrentRevision!.Id }, default));
        Assert.Equal("TEMPLATE_NOT_SELECTABLE", error.ErrorCode);
    }

    [PostgresFact]
    public async Task AuditFailureRollsBackShopAndIdempotencyRecord()
    {
        var (owner, _) = await SeedAsync(); Guid id;
        await using (var create = fixture.Create()) id = (await Service(create).CreateAsync(owner, Guid.NewGuid().ToString(), Request(), default)).ShopId;
        await using (var write = fixture.Create(new AuditFailureInterceptor())) {
            await Assert.ThrowsAsync<DbUpdateException>(() => Service(write).UpdateAsync(owner, Guid.NewGuid().ToString(),
                new() { ExpectedVersion = 1, Name = "Must roll back" }, default));
        }
        await using var read = fixture.Create();
        Assert.Equal(1, (await read.Shops.SingleAsync(x => x.Id == id)).Version);
        Assert.Equal(1, await read.ShopIdempotencyRecords.CountAsync(x => x.ActorUserId == owner));
        Assert.Equal(1, await read.AuditLogs.CountAsync(x => x.ActorUserId == owner && x.EntityName == "ShopManagement"));
    }

    private sealed class AuditFailureInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (command.CommandText.Contains("INSERT INTO audit_logs", StringComparison.Ordinal)) throw new InvalidOperationException("Injected audit persistence failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class LockAttemptInterceptor : DbCommandInterceptor
    {
        internal TaskCompletionSource Attempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (command.CommandText.Contains("FROM users", StringComparison.Ordinal) && command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) Attempt.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
    private sealed class ShopQueryCounter : DbCommandInterceptor
    {
        internal int Reads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            Reads++; return ValueTask.FromResult(result);
        }
    }
}
