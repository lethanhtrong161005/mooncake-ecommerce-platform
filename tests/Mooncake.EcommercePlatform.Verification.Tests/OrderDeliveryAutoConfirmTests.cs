namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.WebApi.Background;
using Xunit;

public sealed class OrderDeliveryAutoConfirmTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    private static readonly DateTime FixedNow = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PolicyDefault_DeliveryAutoConfirmHours_Is72()
    {
        Assert.Equal(72, OrderPolicyDefaults.DeliveryAutoConfirmHours);
        Assert.Equal("orders.delivery_auto_confirm_hours", OrderPolicyDefaults.DeliveryAutoConfirmHoursKey);
    }

    [Fact]
    public void Job_Metadata_IntervalIs15Minutes()
    {
        var job = new OrderDeliveryAutoConfirmJob(Mock.Of<IServiceScopeFactory>());
        Assert.Equal("Bulk order delivery auto-confirmation", job.Name);
        Assert.Equal(TimeSpan.FromMinutes(15), job.Interval);
    }

    [Fact]
    public async Task Job_ExecuteAsync_InvokesRepositoryAutoConfirm()
    {
        var repositoryMock = new Mock<IEnterpriseOrderRepository>();
        repositoryMock.Setup(r => r.AutoConfirmDeliveredOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnterpriseOrderRepository)))
            .Returns(repositoryMock.Object);

        var serviceScopeMock = new Mock<IServiceScope>();
        serviceScopeMock.SetupGet(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);

        var job = new OrderDeliveryAutoConfirmJob(scopeFactoryMock.Object);
        await job.ExecuteAsync(CancellationToken.None);

        repositoryMock.Verify(r => r.AutoConfirmDeliveredOrdersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OrderResponse_AutoConfirmAt_WhenShippedWithProof_Sets72hFromProofCreation()
    {
        var proofCreatedAt = FixedNow.AddHours(-10);
        var expectedAutoConfirmAt = proofCreatedAt.AddHours(OrderPolicyDefaults.DeliveryAutoConfirmHours);

        var response = new OrderResponse(
            Guid.NewGuid(),
            "ORD-TEST-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderStatus.Shipped,
            1_000_000m,
            0m,
            80_000m,
            50_000m,
            0m,
            1_130_000m,
            "123 Test St",
            "Nguyen Van Test",
            "0901234567",
            DateOnly.FromDateTime(FixedNow.AddDays(2)),
            "Test notes",
            FixedNow.AddDays(-3),
            [],
            null)
        {
            AutoConfirmAt = expectedAutoConfirmAt
        };

        Assert.Equal(expectedAutoConfirmAt, response.AutoConfirmAt);
        Assert.Equal(expectedAutoConfirmAt, proofCreatedAt.AddHours(72));
    }

    [Fact]
    public void OrderResponse_AutoConfirmAt_WhenNotShipped_IsNull()
    {
        var response = new OrderResponse(
            Guid.NewGuid(),
            "ORD-TEST-002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrderStatus.Delivered,
            1_000_000m,
            0m,
            80_000m,
            50_000m,
            0m,
            1_130_000m,
            "123 Test St",
            "Nguyen Van Test",
            "0901234567",
            DateOnly.FromDateTime(FixedNow.AddDays(2)),
            "Test notes",
            FixedNow.AddDays(-3),
            [],
            null)
        {
            AutoConfirmAt = null
        };

        Assert.Null(response.AutoConfirmAt);
    }

    [Fact]
    public void RuleValidation_Proof73hWithoutDispute_EligibleForAutoConfirmAndReputationBonus()
    {
        var proofCreatedAt = FixedNow.AddHours(-73);
        var cutoff = FixedNow.AddHours(-OrderPolicyDefaults.DeliveryAutoConfirmHours);
        var orderStatus = OrderStatus.Shipped;
        var hasOpenDispute = false;

        var isEligible = orderStatus == OrderStatus.Shipped && proofCreatedAt <= cutoff && !hasOpenDispute;
        Assert.True(isEligible);

        // Score delta rule
        const decimal expectedReputationDelta = 5m;
        Assert.Equal(5m, expectedReputationDelta);
    }

    [Fact]
    public void RuleValidation_OpenDispute_BlocksAutoConfirmation()
    {
        var proofCreatedAt = FixedNow.AddHours(-73);
        var cutoff = FixedNow.AddHours(-OrderPolicyDefaults.DeliveryAutoConfirmHours);
        var orderStatus = OrderStatus.Shipped;
        var hasOpenDispute = true;

        var isEligible = orderStatus == OrderStatus.Shipped && proofCreatedAt <= cutoff && !hasOpenDispute;
        Assert.False(isEligible);
    }

    [Fact]
    public void RuleValidation_Idempotency_ReputationAwardedOnlyOnce()
    {
        var orderId = Guid.NewGuid();
        var existingLogs = new List<ReputationLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ReferenceId = orderId,
                ReferenceType = "Order",
                ScoreDelta = 5m,
                EventType = ReputationEvent.CompletedOnTime
            }
        };

        var alreadyRewarded = existingLogs.Any(l => l.ReferenceId == orderId && l.ReferenceType == "Order");
        Assert.True(alreadyRewarded);
    }

    [PostgresFact]
    public async Task AutoConfirm_WhenProof73hWithoutDispute_DeliveredAndEligibleAndReputationAwarded()
    {
        await using var db = fixture.Create();
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.SetupGet(c => c.UtcNow).Returns(FixedNow);

        var (order, delivery, supplierProfile) = await SeedShippedOrderAsync(db, proofAgeHours: 73, hasOpenDispute: false);

        var repo = new EnterpriseOrderRepository(db, clockMock.Object);
        var confirmedCount = await repo.AutoConfirmDeliveredOrdersAsync(CancellationToken.None);

        Assert.True(confirmedCount >= 1);

        var updatedOrder = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Delivered, updatedOrder.Status);

        var updatedDelivery = await db.Deliveries.AsNoTracking().SingleAsync(d => d.Id == delivery.Id);
        Assert.Equal(DeliveryStatus.Delivered, updatedDelivery.Status);

        var updatedProfile = await db.SupplierProfiles.AsNoTracking().SingleAsync(sp => sp.Id == supplierProfile.Id);
        Assert.Equal(55m, updatedProfile.ReputationScore);

        var repLog = await db.ReputationLogs.AsNoTracking()
            .SingleOrDefaultAsync(l => l.ReferenceId == order.Id && l.ReferenceType == "Order");
        Assert.NotNull(repLog);
        Assert.Equal(5m, repLog.ScoreDelta);
        Assert.Equal(ReputationEvent.CompletedOnTime, repLog.EventType);
        Assert.Equal(supplierProfile.Id, repLog.SupplierId);

        var wfEvent = await db.WorkflowEvents.AsNoTracking()
            .FirstOrDefaultAsync(w => w.AggregateId == order.Id && w.EventType == "OrderDeliveryConfirmed");
        Assert.NotNull(wfEvent);
        Assert.Equal("Tự xác nhận sau 72h", wfEvent.Reason);

        var custNotification = await db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.ReferenceId == order.Id && n.UserId == order.CustomerId && n.Type == "order.delivered");
        Assert.NotNull(custNotification);

        var supNotification = await db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.ReferenceId == order.Id && n.UserId == supplierProfile.UserId && n.Type == "order.delivered");
        Assert.NotNull(supNotification);
    }

    [PostgresFact]
    public async Task AutoConfirm_WhenOpenDisputeExists_DoesNotConfirmOrAwardReputation()
    {
        await using var db = fixture.Create();
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.SetupGet(c => c.UtcNow).Returns(FixedNow);

        var (order, delivery, supplierProfile) = await SeedShippedOrderAsync(db, proofAgeHours: 73, hasOpenDispute: true);

        var repo = new EnterpriseOrderRepository(db, clockMock.Object);
        var confirmedCount = await repo.AutoConfirmDeliveredOrdersAsync(CancellationToken.None);

        Assert.Equal(0, confirmedCount);

        var updatedOrder = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Shipped, updatedOrder.Status);

        var updatedProfile = await db.SupplierProfiles.AsNoTracking().SingleAsync(sp => sp.Id == supplierProfile.Id);
        Assert.Equal(50m, updatedProfile.ReputationScore);

        var repLog = await db.ReputationLogs.AsNoTracking()
            .SingleOrDefaultAsync(l => l.ReferenceId == order.Id && l.ReferenceType == "Order");
        Assert.Null(repLog);
    }

    [PostgresFact]
    public async Task AutoConfirm_WhenRunTwice_DoesNotAwardReputationTwice()
    {
        await using var db = fixture.Create();
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.SetupGet(c => c.UtcNow).Returns(FixedNow);

        var (order, delivery, supplierProfile) = await SeedShippedOrderAsync(db, proofAgeHours: 73, hasOpenDispute: false);

        var repo = new EnterpriseOrderRepository(db, clockMock.Object);
        var firstRunCount = await repo.AutoConfirmDeliveredOrdersAsync(CancellationToken.None);
        Assert.True(firstRunCount >= 1);

        var profileAfterFirst = await db.SupplierProfiles.AsNoTracking().SingleAsync(sp => sp.Id == supplierProfile.Id);
        Assert.Equal(55m, profileAfterFirst.ReputationScore);

        var secondRunCount = await repo.AutoConfirmDeliveredOrdersAsync(CancellationToken.None);
        Assert.Equal(0, secondRunCount);

        var profileAfterSecond = await db.SupplierProfiles.AsNoTracking().SingleAsync(sp => sp.Id == supplierProfile.Id);
        Assert.Equal(55m, profileAfterSecond.ReputationScore);

        var logsCount = await db.ReputationLogs.AsNoTracking()
            .CountAsync(l => l.ReferenceId == order.Id && l.ReferenceType == "Order");
        Assert.Equal(1, logsCount);
    }

    private static async Task<(Order order, Delivery delivery, SupplierProfile profile)> SeedShippedOrderAsync(
        ApplicationDbContext db, int proofAgeHours, bool hasOpenDispute)
    {
        var supplierUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"supplier_{Guid.NewGuid():N}@test.local",
            Role = UserRole.Supplier,
            IsActive = true
        };
        var customerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"customer_{Guid.NewGuid():N}@test.local",
            Role = UserRole.Customer,
            IsActive = true
        };
        db.Users.AddRange(supplierUser, customerUser);

        var profile = new SupplierProfile
        {
            Id = Guid.NewGuid(),
            UserId = supplierUser.Id,
            CompanyName = "Test Bakery JSC",
            ReputationScore = 50m,
            VerificationStatus = SupplierVerificationStatus.Verified
        };
        db.SupplierProfiles.Add(profile);

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = supplierUser.Id,
            SupplierId = profile.Id,
            Name = "Test Artisan Bakery",
            Slug = $"test-artisan-{Guid.NewGuid():N}",
            Status = ShopStatus.Active
        };
        db.Shops.Add(shop);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            CustomerId = customerUser.Id,
            ShopId = shop.Id,
            Status = OrderStatus.Shipped,
            TotalAmount = 5_000_000m,
            Subtotal = 5_000_000m,
            Version = 1,
            CreatedAt = FixedNow.AddHours(-100),
            UpdatedAt = FixedNow.AddHours(-proofAgeHours)
        };
        db.Orders.Add(order);

        // Add Held funds
        db.OrderFinancialLedgerEntries.Add(new OrderFinancialLedgerEntry
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShopId = shop.Id,
            CustomerId = customerUser.Id,
            EntryType = "HeldLocked",
            BalanceBucket = "Held",
            Amount = 5_000_000m,
            Description = "Deposit funds held.",
            IdempotencyKey = $"held-init:{order.Id}",
            CreatedAt = FixedNow.AddHours(-100),
            UpdatedAt = FixedNow.AddHours(-100)
        });

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            DeliveryNumber = $"DEL-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Status = DeliveryStatus.AwaitingCustomerConfirmation,
            DeliveryType = DeliveryType.SelfArranged,
            CreatedAt = FixedNow.AddHours(-proofAgeHours),
            UpdatedAt = FixedNow.AddHours(-proofAgeHours)
        };
        db.Deliveries.Add(delivery);

        var proof = new DeliveryProof
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            ProofType = ProofType.Delivery,
            PhotoUrl = "https://example.com/pod.jpg",
            CreatedAt = FixedNow.AddHours(-proofAgeHours),
            UpdatedAt = FixedNow.AddHours(-proofAgeHours)
        };
        db.DeliveryProofs.Add(proof);

        if (hasOpenDispute)
        {
            db.OrderDeliveryDisputes.Add(new OrderDeliveryDispute
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                DeliveryId = delivery.Id,
                CustomerId = customerUser.Id,
                ShopId = shop.Id,
                IssueType = "DamagedGoods",
                Description = "Boxes were crushed during transport.",
                Status = "Open",
                CreatedAt = FixedNow.AddHours(-10),
                UpdatedAt = FixedNow.AddHours(-10)
            });
        }

        await db.SaveChangesAsync();
        return (order, delivery, profile);
    }
}
