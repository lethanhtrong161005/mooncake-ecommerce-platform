namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.WebApi.Background;
using Xunit;

public sealed class SupplierLicenseExpiryTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    private static readonly DateTime FixedNowUtc = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly TodayVn = new(2026, 11, 15);

    [Fact]
    public void Job_Metadata_IntervalIs1Hour()
    {
        var job = new SupplierLicenseExpiryJob(Mock.Of<IServiceScopeFactory>());
        Assert.Equal(TimeSpan.FromHours(1), job.Interval);
        Assert.False(string.IsNullOrWhiteSpace(job.Name));
    }

    [Fact]
    public async Task Job_ExecuteAsync_InvokesRepositoryLicenseExpiryProcessing()
    {
        var repository = new Mock<ISupplierRepository>();
        repository.Setup(r => r.ProcessLicenseExpiriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var provider = new Mock<IServiceProvider>();
        provider.Setup(sp => sp.GetService(typeof(ISupplierRepository))).Returns(repository.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        await new SupplierLicenseExpiryJob(factory.Object).ExecuteAsync(CancellationToken.None);

        repository.Verify(r => r.ProcessLicenseExpiriesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SupplierService_Review_WhenApprovingNewValidFoodSafetyDoc_CallsUnsuspendShop()
    {
        var repository = new Mock<ISupplierRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(FixedNowUtc);

        var adminUser = new User { Id = Guid.NewGuid(), Role = UserRole.Admin, IsActive = true };
        var supplierUser = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = FixedNowUtc };
        var profile = new SupplierProfile { Id = Guid.NewGuid(), UserId = supplierUser.Id, Version = 1, VerificationStatus = SupplierVerificationStatus.Pending };
        var submission = new SupplierVerificationSubmission { Id = Guid.NewGuid(), SupplierProfileId = profile.Id, Revision = 1, Status = SupplierVerificationStatus.Pending };
        profile.CurrentSubmissionId = submission.Id;

        repository.Setup(r => r.GetUserAsync(adminUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(adminUser);
        repository.Setup(r => r.GetUserAsync(supplierUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supplierUser);
        repository.Setup(r => r.GetProfileByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        repository.Setup(r => r.GetProfileByUserIdAsync(supplierUser.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        repository.Setup(r => r.GetSubmissionAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var docs = new List<SupplierVerificationDocument>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerUserId = supplierUser.Id,
                SupplierProfileId = profile.Id,
                SubmissionId = submission.Id,
                DocumentType = SupplierVerificationConstants.BusinessLicense,
                UploadStatus = SupplierVerificationConstants.Ready,
                FileSizeBytes = 100
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerUserId = supplierUser.Id,
                SupplierProfileId = profile.Id,
                SubmissionId = submission.Id,
                DocumentType = SupplierVerificationConstants.FoodSafety,
                ExpiresOn = TodayVn.AddDays(180),
                UploadStatus = SupplierVerificationConstants.Ready,
                FileSizeBytes = 100
            }
        };

        repository.Setup(r => r.GetDocumentsAsync(profile.Id, submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        repository.Setup(r => r.GetSelectedDocumentsAsync(profile.Id, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        repository.Setup(r => r.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<Func<CancellationToken, Task<VerificationCommandResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<Guid> _, Func<CancellationToken, Task<VerificationCommandResponse>> work, CancellationToken ct) => work(ct));

        var service = new SupplierService(repository.Object, clock.Object, Mock.Of<ICloudinaryStorageService>(),
            Mock.Of<IVerificationDocumentGateway>(), Mock.Of<ITraceContext>());

        var key = Guid.NewGuid().ToString("N");
        var request = new ReviewSupplierRequest
        {
            SubmissionId = submission.Id,
            ExpectedVersion = 1,
            Approve = true,
            RejectionReason = null
        };
        await service.ReviewAsync(profile.Id, adminUser.Id, key, request, CancellationToken.None);

        repository.Verify(r => r.UnsuspendShopIfSuspendedForExpiredLicenseAsync(profile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SupplierService_Review_WhenRejectingSubmission_DoesNotCallUnsuspendShop()
    {
        var repository = new Mock<ISupplierRepository>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(FixedNowUtc);

        var adminUser = new User { Id = Guid.NewGuid(), Role = UserRole.Admin, IsActive = true };
        var supplierUser = new User { Id = Guid.NewGuid(), Role = UserRole.Customer, RequestedRole = UserRole.Supplier, IsActive = true, EmailVerifiedAt = FixedNowUtc };
        var profile = new SupplierProfile { Id = Guid.NewGuid(), UserId = supplierUser.Id, Version = 1, VerificationStatus = SupplierVerificationStatus.Pending };
        var submission = new SupplierVerificationSubmission { Id = Guid.NewGuid(), SupplierProfileId = profile.Id, Revision = 1, Status = SupplierVerificationStatus.Pending };
        profile.CurrentSubmissionId = submission.Id;

        repository.Setup(r => r.GetUserAsync(adminUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(adminUser);
        repository.Setup(r => r.GetUserAsync(supplierUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supplierUser);
        repository.Setup(r => r.GetProfileByIdAsync(profile.Id, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        repository.Setup(r => r.GetProfileByUserIdAsync(supplierUser.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        repository.Setup(r => r.GetSubmissionAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var docs = new List<SupplierVerificationDocument>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerUserId = supplierUser.Id,
                SupplierProfileId = profile.Id,
                SubmissionId = submission.Id,
                DocumentType = SupplierVerificationConstants.FoodSafety,
                ExpiresOn = TodayVn.AddDays(-5),
                UploadStatus = SupplierVerificationConstants.Ready,
                FileSizeBytes = 100
            }
        };

        repository.Setup(r => r.GetDocumentsAsync(profile.Id, submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        repository.Setup(r => r.GetSelectedDocumentsAsync(profile.Id, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        repository.Setup(r => r.InTransactionAsync(It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<Func<CancellationToken, Task<VerificationCommandResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<Guid> _, Func<CancellationToken, Task<VerificationCommandResponse>> work, CancellationToken ct) => work(ct));

        var service = new SupplierService(repository.Object, clock.Object, Mock.Of<ICloudinaryStorageService>(),
            Mock.Of<IVerificationDocumentGateway>(), Mock.Of<ITraceContext>());

        var key = Guid.NewGuid().ToString("N");
        var request = new ReviewSupplierRequest
        {
            SubmissionId = submission.Id,
            ExpectedVersion = 1,
            Approve = false,
            RejectionReason = "Documents are blurry and expired."
        };
        await service.ReviewAsync(profile.Id, adminUser.Id, key, request, CancellationToken.None);

        repository.Verify(r => r.UnsuspendShopIfSuspendedForExpiredLicenseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [PostgresFact]
    public async Task ProcessLicenseExpiries_T30_SendsWarningOnce()
    {
        await using var db = fixture.Create();
        var seed = await SeedSupplierAsync(db);
        var clock = CreateClock();
        var repository = new SupplierRepository(db, clock.Object);

        var docT30 = new SupplierVerificationDocument
        {
            Id = Guid.NewGuid(),
            OwnerUserId = seed.SupplierUser.Id,
            SupplierProfileId = seed.Profile.Id,
            UploadOperationId = Guid.NewGuid(),
            DocumentType = SupplierVerificationConstants.FoodSafety,
            ReviewStatus = "Approved",
            ExpiresOn = TodayVn.AddDays(30),
            UploadStatus = SupplierVerificationConstants.Ready,
            CreatedAt = FixedNowUtc.AddDays(-30)
        };
        var docFar = new SupplierVerificationDocument
        {
            Id = Guid.NewGuid(),
            OwnerUserId = seed.SupplierUser.Id,
            SupplierProfileId = seed.Profile.Id,
            UploadOperationId = Guid.NewGuid(),
            DocumentType = SupplierVerificationConstants.FoodSafety,
            ReviewStatus = "Approved",
            ExpiresOn = TodayVn.AddDays(45),
            UploadStatus = SupplierVerificationConstants.Ready,
            CreatedAt = FixedNowUtc.AddDays(-30)
        };
        db.SupplierVerificationDocuments.AddRange(docT30, docFar);
        await db.SaveChangesAsync();

        // Run twice to test idempotency
        await repository.ProcessLicenseExpiriesAsync(CancellationToken.None);
        await repository.ProcessLicenseExpiriesAsync(CancellationToken.None);

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.Type == "supplier.license.attp_expiry_t30")
            .ToListAsync();

        var notification = Assert.Single(notifications);
        Assert.Equal(docT30.Id, notification.ReferenceId);
        Assert.Equal(seed.SupplierUser.Id, notification.UserId);
    }

    [PostgresFact]
    public async Task ProcessLicenseExpiries_Expired_SuspendsShopAndNotifiesSupplierAndAdmin()
    {
        await using var db = fixture.Create();
        var seed = await SeedSupplierAsync(db);
        var clock = CreateClock();
        var repository = new SupplierRepository(db, clock.Object);

        var expiredDoc = new SupplierVerificationDocument
        {
            Id = Guid.NewGuid(),
            OwnerUserId = seed.SupplierUser.Id,
            SupplierProfileId = seed.Profile.Id,
            UploadOperationId = Guid.NewGuid(),
            DocumentType = SupplierVerificationConstants.FoodSafety,
            ReviewStatus = "Approved",
            ExpiresOn = TodayVn.AddDays(-1), // Expired yesterday
            UploadStatus = SupplierVerificationConstants.Ready,
            CreatedAt = FixedNowUtc.AddDays(-60)
        };
        db.SupplierVerificationDocuments.Add(expiredDoc);
        await db.SaveChangesAsync();

        // First run
        await repository.ProcessLicenseExpiriesAsync(CancellationToken.None);

        var updatedShop = await db.Shops.AsNoTracking().SingleAsync(s => s.Id == seed.Shop.Id);
        Assert.Equal(ShopStatus.Suspended, updatedShop.Status);
        Assert.Equal("Giấy ATTP hết hạn", updatedShop.SuspensionReason);
        Assert.Null(updatedShop.SuspendedByUserId);
        Assert.NotNull(updatedShop.SuspendedAtUtc);

        var supplierNotifications = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == seed.SupplierUser.Id && n.Type == "supplier.license.attp_expired")
            .ToListAsync();
        Assert.Single(supplierNotifications);

        var adminNotifications = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == seed.AdminUser.Id && n.Type == "admin.supplier.license.attp_expired")
            .ToListAsync();
        Assert.Single(adminNotifications);

        // Second run - idempotent: no duplicate notifications or errors
        await repository.ProcessLicenseExpiriesAsync(CancellationToken.None);

        var supplierNotifsAfter = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == seed.SupplierUser.Id && n.Type == "supplier.license.attp_expired")
            .ToListAsync();
        Assert.Single(supplierNotifsAfter);
    }

    [PostgresFact]
    public async Task ProcessLicenseExpiries_Expired_WhenSupplierHasAnotherValidDoc_DoesNotSuspendShop()
    {
        await using var db = fixture.Create();
        var seed = await SeedSupplierAsync(db);
        var clock = CreateClock();
        var repository = new SupplierRepository(db, clock.Object);

        var expiredDoc = new SupplierVerificationDocument
        {
            Id = Guid.NewGuid(),
            OwnerUserId = seed.SupplierUser.Id,
            SupplierProfileId = seed.Profile.Id,
            UploadOperationId = Guid.NewGuid(),
            DocumentType = SupplierVerificationConstants.FoodSafety,
            ReviewStatus = "Approved",
            ExpiresOn = TodayVn.AddDays(-2),
            UploadStatus = SupplierVerificationConstants.Ready,
            CreatedAt = FixedNowUtc.AddDays(-60)
        };
        var validDoc = new SupplierVerificationDocument
        {
            Id = Guid.NewGuid(),
            OwnerUserId = seed.SupplierUser.Id,
            SupplierProfileId = seed.Profile.Id,
            UploadOperationId = Guid.NewGuid(),
            DocumentType = SupplierVerificationConstants.FoodSafety,
            ReviewStatus = "Approved",
            ExpiresOn = TodayVn.AddDays(90),
            UploadStatus = SupplierVerificationConstants.Ready,
            CreatedAt = FixedNowUtc.AddDays(-10)
        };
        db.SupplierVerificationDocuments.AddRange(expiredDoc, validDoc);
        await db.SaveChangesAsync();

        await repository.ProcessLicenseExpiriesAsync(CancellationToken.None);

        var shop = await db.Shops.AsNoTracking().SingleAsync(s => s.Id == seed.Shop.Id);
        Assert.Equal(ShopStatus.Active, shop.Status);
        Assert.Null(shop.SuspensionReason);
    }

    [PostgresFact]
    public async Task UnsuspendShop_WhenSuspendedForExpiredLicense_RestoresToActive()
    {
        await using var db = fixture.Create();
        var seed = await SeedSupplierAsync(db);
        var clock = CreateClock();
        var repository = new SupplierRepository(db, clock.Object);

        // Set shop to suspended due to expired license
        seed.Shop.Status = ShopStatus.Suspended;
        seed.Shop.PreSuspensionStatus = ShopStatus.Active;
        seed.Shop.SuspensionReason = "Giấy ATTP hết hạn";
        seed.Shop.SuspendedByUserId = null;
        seed.Shop.SuspendedAtUtc = FixedNowUtc.AddDays(-1);
        await db.SaveChangesAsync();

        var count = await repository.UnsuspendShopIfSuspendedForExpiredLicenseAsync(seed.Profile.Id, CancellationToken.None);
        Assert.Equal(1, count);

        var restoredShop = await db.Shops.AsNoTracking().SingleAsync(s => s.Id == seed.Shop.Id);
        Assert.Equal(ShopStatus.Active, restoredShop.Status);
        Assert.Null(restoredShop.SuspensionReason);
        Assert.Null(restoredShop.SuspendedByUserId);
        Assert.Null(restoredShop.SuspendedAtUtc);
    }

    [PostgresFact]
    public async Task UnsuspendShop_WhenSuspendedByAdminForOtherReason_DoesNotUnsuspend()
    {
        await using var db = fixture.Create();
        var seed = await SeedSupplierAsync(db);
        var clock = CreateClock();
        var repository = new SupplierRepository(db, clock.Object);

        // Set shop to suspended by admin for policy violation
        seed.Shop.Status = ShopStatus.Suspended;
        seed.Shop.PreSuspensionStatus = ShopStatus.Active;
        seed.Shop.SuspensionReason = "Violated marketplace policy";
        seed.Shop.SuspendedByUserId = seed.AdminUser.Id;
        seed.Shop.SuspendedAtUtc = FixedNowUtc.AddDays(-1);
        await db.SaveChangesAsync();

        var count = await repository.UnsuspendShopIfSuspendedForExpiredLicenseAsync(seed.Profile.Id, CancellationToken.None);
        Assert.Equal(0, count);

        var shop = await db.Shops.AsNoTracking().SingleAsync(s => s.Id == seed.Shop.Id);
        Assert.Equal(ShopStatus.Suspended, shop.Status);
        Assert.Equal("Violated marketplace policy", shop.SuspensionReason);
        Assert.Equal(seed.AdminUser.Id, shop.SuspendedByUserId);
    }

    private static Mock<IDateTimeProvider> CreateClock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(FixedNowUtc);
        return clock;
    }

    private static async Task<(User SupplierUser, User AdminUser, SupplierProfile Profile, Shop Shop)> SeedSupplierAsync(ApplicationDbContext db)
    {
        var supplierUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"supplier_{Guid.NewGuid():N}@test.local",
            Role = UserRole.Supplier,
            IsActive = true
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"admin_{Guid.NewGuid():N}@test.local",
            Role = UserRole.Admin,
            IsActive = true
        };
        db.Users.AddRange(supplierUser, adminUser);

        var profile = new SupplierProfile
        {
            Id = Guid.NewGuid(),
            UserId = supplierUser.Id,
            CompanyName = "Mooncake Supplier Co",
            VerificationStatus = SupplierVerificationStatus.Verified
        };
        db.SupplierProfiles.Add(profile);

        var (templateId, revisionId) = await ShopTestHelper.EnsurePublishedTemplateRevisionAsync(db, FixedNowUtc);

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = supplierUser.Id,
            SupplierId = profile.Id,
            TemplateId = templateId,
            TemplateRevisionId = revisionId,
            Name = "Mooncake Gourmet Shop",
            Slug = $"shop-{Guid.NewGuid():N}"[..16],
            Status = ShopStatus.Active,
            CreatedAt = FixedNowUtc.AddDays(-60),
            UpdatedAt = FixedNowUtc.AddDays(-60)
        };
        db.Shops.Add(shop);

        await db.SaveChangesAsync();
        return (supplierUser, adminUser, profile, shop);
    }
}
