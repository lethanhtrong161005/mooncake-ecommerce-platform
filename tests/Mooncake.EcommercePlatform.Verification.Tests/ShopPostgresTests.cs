namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;

public sealed partial class ShopPostgresTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    private static readonly Guid[] Templates = Enumerable.Range(1, 7).Select(i => Guid.Parse($"90000000-0000-4000-8000-{i:D12}")).ToArray();
    private static readonly Guid[] Revisions = Enumerable.Range(1, 7).Select(i => Guid.Parse($"91000000-0000-4000-8000-{i:D12}")).ToArray();
    private static ShopService Service(ApplicationDbContext db, IShopMediaGateway? media = null)
    {
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(() => DateTime.UtcNow);
        return new(new ShopRepository(db), clock.Object, new ShopCursorProtector(new EphemeralDataProtectionProvider(), clock.Object), media ?? Mock.Of<IShopMediaGateway>());
    }
    internal async Task<(Guid Owner, Guid Admin)> SeedAsync()
    {
        await using var db = fixture.Create();
        if (!await db.ShopTemplateCatalogStates.AnyAsync()) {
            var defaults = new ThemeOverridesRequest { PrimaryColor = "#8C1524", BackgroundColor = "#FAF8F6", TextColor = "#222222",
                SurfaceColor = "#FFFFFF", ButtonTextColor = "#FFFFFF", BorderRadiusPx = 12, FontScale = 1 };
            for (var i = 0; i < Templates.Length; i++) db.ShopTemplates.Add(new() {
                Id = Templates[i], Code = $"template-{i}", Name = $"Template {i}", IsActive = false, Version = 1, SortOrder = i });
            await db.SaveChangesAsync();
            for (var i = 0; i < Templates.Length; i++) db.ShopTemplateRevisions.Add(new() {
                Id = Revisions[i], TemplateId = Templates[i], Revision = 1, RendererKey = "heritage", SchemaVersion = 1,
                CssVariables = ShopPolicyHelper.Serialize(defaults), LayoutConfig = ShopPolicyHelper.Serialize(new TemplateLayoutRequest {
                    SectionOrder = ["Hero", "Products", "Contact"], ProductColumnsDesktop = 3 }) });
            await db.SaveChangesAsync();
            for (var i = 0; i < Templates.Length; i++) {
                var template = await db.ShopTemplates.SingleAsync(x => x.Id == Templates[i]);
                template.CurrentRevisionId = Revisions[i]; template.IsActive = true;
            }
            db.ShopTemplateCatalogStates.Add(new() { Id = 1, DefaultTemplateId = Templates[0] });
            await db.SaveChangesAsync();
        }
        var owner = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", Role = UserRole.Supplier,
            RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = DateTime.UtcNow };
        var admin = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.test", Role = UserRole.Admin, IsActive = true };
        db.Users.AddRange(owner, admin);
        db.SupplierProfiles.Add(new() { Id = Guid.NewGuid(), UserId = owner.Id, CompanyName = "Test Bakery", VerificationStatus = SupplierVerificationStatus.Verified });
        await db.SaveChangesAsync();
        return (owner.Id, admin.Id);
    }
    private static CreateShopRequest Request(string? slug = null) => new() {
        Name = "Test Bakery", Slug = slug ?? $"bakery-{Guid.NewGuid():N}", TemplateId = Templates[0], TemplateRevisionId = Revisions[0] };

    [PostgresFact]
    public async Task ConcurrentCreatesProduceOneShopAndOneConflict()
    {
        var (owner, _) = await SeedAsync();
        async Task<int> Create() {
            await using var db = fixture.Create();
            try { await Service(db).CreateAsync(owner, Guid.NewGuid().ToString(), Request(), default); return 201; }
            catch (HttpException failure) { return failure.StatusCode; }
        }
        Assert.Equal(new[] { 201, 409 }, (await Task.WhenAll(Create(), Create())).Order().ToArray());
        await using var read = fixture.Create();
        Assert.Equal(1, await read.Shops.CountAsync(x => x.OwnerUserId == owner));
        Assert.Equal(1, await read.AuditLogs.CountAsync(x => x.ActorUserId == owner && x.Action == "ShopCreated"));
    }

    [PostgresFact]
    public async Task CommittedCreateReplayDoesNotDuplicateAudits()
    {
        var (owner, _) = await SeedAsync(); var key = Guid.NewGuid().ToString(); var request = Request();
        await using (var first = fixture.Create()) await Service(first).CreateAsync(owner, key, request, default);
        await using var replay = fixture.Create();
        var result = await Service(replay).CreateAsync(owner, key, request, default);
        Assert.True(result.IdempotentReplay);
        Assert.Equal(1, result.Version);
        Assert.Equal(1, await replay.AuditLogs.CountAsync(x => x.ActorUserId == owner && x.Action == "ShopCreated"));
    }

    [PostgresFact]
    public async Task SuspensionCannotBeOverwrittenAndRestorePreservesInactive()
    {
        var (owner, admin) = await SeedAsync();
        Guid id;
        await using (var create = fixture.Create()) id = (await Service(create).CreateAsync(owner, Guid.NewGuid().ToString(), Request(), default)).ShopId;
        await using (var suspend = fixture.Create()) {
            var result = await Service(suspend).ModerateAsync(admin, id, Guid.NewGuid().ToString(), new() {
                ExpectedVersion = 1, Action = "Suspend", Reason = "Supplier policy needs further review." }, default);
            Assert.Equal(ShopStatus.Suspended, result.Status);
        }
        await using (var stale = fixture.Create()) {
            var failure = await Assert.ThrowsAsync<HttpException>(() => Service(stale).VisibilityAsync(owner, Guid.NewGuid().ToString(), new() {
                ExpectedVersion = 2, Status = ShopStatus.Active }, default));
            Assert.Equal("SHOP_SUSPENDED", failure.ErrorCode);
        }
        await using var restore = fixture.Create();
        var restored = await Service(restore).ModerateAsync(admin, id, Guid.NewGuid().ToString(), new() {
            ExpectedVersion = 2, Action = "Restore", Reason = "The policy review has been completed." }, default);
        Assert.Equal(ShopStatus.Inactive, restored.Status);
        Assert.Equal(3, restored.Version);
    }

    [PostgresFact]
    public async Task PublicVariantFiltersMustMatchOneVariantAndUseBoundedQueries()
    {
        var (owner, _) = await SeedAsync(); var request = Request(); Guid shopId;
        await using (var setup = fixture.Create()) {
            var service = Service(setup);
            var shop = await service.CreateAsync(owner, Guid.NewGuid().ToString(), request, default); shopId = shop.ShopId;
            await service.VisibilityAsync(owner, Guid.NewGuid().ToString(), new() { ExpectedVersion = 1, Status = ShopStatus.Active }, default);
            var category = new Category { Id = Guid.NewGuid(), Name = "Mooncakes" }; setup.Categories.Add(category);
            var product = new Product { Id = Guid.NewGuid(), ShopId = shopId, CategoryId = category.Id, Name = "Test Product", BasePrice = 100, MinOrderQty = 1 };
            setup.Products.Add(product);
            setup.ProductVariants.AddRange(new ProductVariant { ProductId = product.Id, Name = "Cheap other filling", Sku = Guid.NewGuid().ToString(), Filling = "Other", PriceAdjustment = -50, StockQty = 10 },
                new ProductVariant { ProductId = product.Id, Name = "Required filling", Sku = Guid.NewGuid().ToString(), Filling = "Lotus", PriceAdjustment = 50, StockQty = 10 });
            await setup.SaveChangesAsync();
        }
        await using var read = fixture.Create();
        var filtered = await Service(read).ProductsAsync(request.Slug, new() { Filling = "Lotus", MaxPrice = 100 }, default);
        Assert.Empty(filtered.Items);
        var match = await Service(read).ProductsAsync(request.Slug, new() { Filling = "Lotus", MinPrice = 100, Sort = "priceAsc", PageSize = 1 }, default);
        Assert.Single(match.Items); Assert.Equal(150, match.Items[0].MinPrice);
        var profile = await Service(read).PublicAsync(request.Slug, default);
        Assert.True(profile.IsVerified); Assert.Equal(0, profile.ReviewCount);
    }

    [PostgresFact]
    public async Task TwoAdministratorsCannotHideBelowFiveSelectableTemplates()
    {
        var (_, admin) = await SeedAsync();
        await using (var setup = fixture.Create()) {
            foreach (var template in await setup.ShopTemplates.Where(x => x.Id != Templates[6]).ToArrayAsync()) template.IsActive = true;
            (await setup.ShopTemplates.SingleAsync(x => x.Id == Templates[6])).IsActive = false;
            await setup.SaveChangesAsync();
        }
        async Task<int> Hide(Guid id) {
            await using var db = fixture.Create();
            var current = await db.ShopTemplates.AsNoTracking().SingleAsync(x => x.Id == id);
            try { await Service(db).TemplateAvailabilityAsync(admin, id, Guid.NewGuid().ToString(), new() { ExpectedVersion = current.Version, IsActive = false }, default); return 200; }
            catch (HttpException failure) { return failure.StatusCode; }
        }
        Assert.Equal(new[] { 200, 409 }, (await Task.WhenAll(Hide(Templates[4]), Hide(Templates[5]))).Order().ToArray());
        await using var read = fixture.Create();
        Assert.Equal(5, await read.ShopTemplates.CountAsync(x => x.IsActive));
    }
}
