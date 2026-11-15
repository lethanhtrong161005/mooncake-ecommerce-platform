namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.WebApi.Background;
using Xunit;

public sealed class ContractMilestoneOverdueTests(PostgresVerificationFixture fixture) : IClassFixture<PostgresVerificationFixture>
{
    // 2026-11-15 12:00 UTC = 19:00 in Vietnam, so "today" (VN) is 2026-11-15.
    private static readonly DateTime FixedNow = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 15);

    [Fact]
    public void Job_Metadata_IntervalIs15Minutes()
    {
        var job = new ContractMilestoneOverdueJob(Mock.Of<IServiceScopeFactory>());
        Assert.Equal(TimeSpan.FromMinutes(15), job.Interval);
        Assert.False(string.IsNullOrWhiteSpace(job.Name));
    }

    [Fact]
    public async Task Job_ExecuteAsync_InvokesRepositoryDeadlineProcessing()
    {
        var repository = new Mock<IContractRepository>();
        repository.Setup(r => r.ProcessMilestoneDeadlinesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var provider = new Mock<IServiceProvider>();
        provider.Setup(sp => sp.GetService(typeof(IContractRepository))).Returns(repository.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);

        await new ContractMilestoneOverdueJob(factory.Object).ExecuteAsync(CancellationToken.None);

        repository.Verify(r => r.ProcessMilestoneDeadlinesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [PostgresFact]
    public async Task ProcessDeadlines_WhenMilestonePastDue_MarksOverdueAndNotifiesBothPartiesOnce()
    {
        await using var db = fixture.Create();
        var seed = await SeedAsync(db, ContractStatus.Active,
            (Today.AddDays(-1), MilestoneStatus.InProgress, false),
            (Today.AddDays(-1), MilestoneStatus.Pending, true),
            (Today.AddDays(-1), MilestoneStatus.PendingReview, false),
            (Today, MilestoneStatus.Pending, false));
        var repository = CreateRepository(db);

        await repository.ProcessMilestoneDeadlinesAsync(CancellationToken.None);
        await repository.ProcessMilestoneDeadlinesAsync(CancellationToken.None);

        var milestones = await db.ContractMilestones.AsNoTracking().Where(m => m.ContractId == seed.Contract.Id).ToListAsync();
        Assert.Equal(MilestoneStatus.Overdue, milestones.Single(m => m.Id == seed.MilestoneIds[0]).Status);
        Assert.Equal(MilestoneStatus.Pending, milestones.Single(m => m.Id == seed.MilestoneIds[1]).Status);
        Assert.Equal(MilestoneStatus.PendingReview, milestones.Single(m => m.Id == seed.MilestoneIds[2]).Status);
        Assert.Equal(MilestoneStatus.Pending, milestones.Single(m => m.Id == seed.MilestoneIds[3]).Status);

        var overdueNotifications = await db.Notifications.AsNoTracking()
            .Where(n => n.ReferenceId == seed.MilestoneIds[0] && n.Type == "contract.milestone.overdue").ToListAsync();
        Assert.Equal(2, overdueNotifications.Count);
        Assert.Contains(overdueNotifications, n => n.UserId == seed.SupplierUserId);
        Assert.Contains(overdueNotifications, n => n.UserId == seed.Contract.CustomerId);
        Assert.Equal(1, await db.WorkflowEvents.AsNoTracking()
            .CountAsync(w => w.AggregateId == seed.MilestoneIds[0] && w.EventType == "MilestoneMarkedOverdue"));
    }

    [PostgresFact]
    public async Task ProcessDeadlines_WhenContractNotActive_DoesNotMarkOverdue()
    {
        await using var db = fixture.Create();
        var seed = await SeedAsync(db, ContractStatus.Disputed, (Today.AddDays(-3), MilestoneStatus.Pending, false));

        await CreateRepository(db).ProcessMilestoneDeadlinesAsync(CancellationToken.None);

        var milestone = await db.ContractMilestones.AsNoTracking().SingleAsync(m => m.Id == seed.MilestoneIds[0]);
        Assert.Equal(MilestoneStatus.Pending, milestone.Status);
    }

    [PostgresFact]
    public async Task ProcessDeadlines_RemindsSupplierOnceAtEachThreshold()
    {
        foreach (var days in new[] { 7, 3, 1 }) await AssertReminderOnceAsync(days);
    }

    private async Task AssertReminderOnceAsync(int days)
    {
        await using var db = fixture.Create();
        var seed = await SeedAsync(db, ContractStatus.Active, (Today.AddDays(days), MilestoneStatus.InProgress, false));
        var repository = CreateRepository(db);

        await repository.ProcessMilestoneDeadlinesAsync(CancellationToken.None);
        await repository.ProcessMilestoneDeadlinesAsync(CancellationToken.None);

        var reminders = await db.Notifications.AsNoTracking()
            .Where(n => n.ReferenceId == seed.MilestoneIds[0] && n.Type == $"contract.milestone.due_t{days}").ToListAsync();
        var reminder = Assert.Single(reminders);
        Assert.Equal(seed.SupplierUserId, reminder.UserId);
        var milestone = await db.ContractMilestones.AsNoTracking().SingleAsync(m => m.Id == seed.MilestoneIds[0]);
        Assert.Equal(MilestoneStatus.InProgress, milestone.Status);
    }

    [PostgresFact]
    public async Task ProcessDeadlines_WhenNotOnThreshold_SendsNoReminder()
    {
        await using var db = fixture.Create();
        var seed = await SeedAsync(db, ContractStatus.Active, (Today.AddDays(5), MilestoneStatus.Pending, false));

        await CreateRepository(db).ProcessMilestoneDeadlinesAsync(CancellationToken.None);

        Assert.False(await db.Notifications.AsNoTracking().AnyAsync(n => n.ReferenceId == seed.MilestoneIds[0]));
    }

    private static ContractRepository CreateRepository(ApplicationDbContext db)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        return new ContractRepository(db, clock.Object);
    }

    private static async Task<(Contract Contract, Guid SupplierUserId, List<Guid> MilestoneIds)> SeedAsync(
        ApplicationDbContext db, ContractStatus status,
        params (DateOnly Due, MilestoneStatus Status, bool IsDeposit)[] milestones)
    {
        var supplierUser = new User { Id = Guid.NewGuid(), Email = $"supplier_{Guid.NewGuid():N}@test.local", Role = UserRole.Supplier, IsActive = true };
        var customerUser = new User { Id = Guid.NewGuid(), Email = $"customer_{Guid.NewGuid():N}@test.local", Role = UserRole.Customer, IsActive = true };
        db.Users.AddRange(supplierUser, customerUser);
        var profile = new SupplierProfile
        {
            Id = Guid.NewGuid(), UserId = supplierUser.Id, CompanyName = "Test Bakery JSC",
            ReputationScore = 50m, VerificationStatus = SupplierVerificationStatus.Verified
        };
        db.SupplierProfiles.Add(profile);
        var contract = new Contract
        {
            Id = Guid.NewGuid(), CustomerId = customerUser.Id, SupplierId = profile.Id,
            ContractNumber = $"CT-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Status = status,
            CreatedAt = FixedNow.AddDays(-30), UpdatedAt = FixedNow.AddDays(-30)
        };
        db.Contracts.Add(contract);
        var ids = new List<Guid>();
        var sequence = 1;
        foreach (var (due, milestoneStatus, isDeposit) in milestones)
        {
            var milestone = new ContractMilestone
            {
                Id = Guid.NewGuid(), ContractId = contract.Id, SequenceNumber = sequence++, Name = $"Milestone {sequence}",
                DueDate = due, Status = milestoneStatus, IsDeposit = isDeposit,
                CreatedAt = FixedNow.AddDays(-30), UpdatedAt = FixedNow.AddDays(-30)
            };
            db.ContractMilestones.Add(milestone);
            ids.Add(milestone.Id);
        }

        await db.SaveChangesAsync();
        return (contract, supplierUser.Id, ids);
    }
}
