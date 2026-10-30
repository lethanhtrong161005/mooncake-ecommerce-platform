namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>EF Core persistence for the custom packaging revision workflow.</summary>
public sealed class CustomPackagingRepository(ApplicationDbContext context) : ICustomPackagingRepository
{
    private const string AggregateType = "CustomPackaging";
    private const string ReferenceType = "custom-packaging";

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        context.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == orderId && !order.IsDeleted,
            cancellationToken);

    public Task<CustomPackaging?> GetByOrderIdAsync(Guid orderId,
        CancellationToken cancellationToken = default) =>
        context.CustomPackagings.AsNoTracking().FirstOrDefaultAsync(packaging =>
            packaging.OrderId == orderId && !packaging.IsDeleted, cancellationToken);

    public Task<CustomPackagingRevision?> GetRevisionAsync(Guid revisionId,
        CancellationToken cancellationToken = default) =>
        context.CustomPackagingRevisions.AsNoTracking().FirstOrDefaultAsync(revision =>
            revision.Id == revisionId && !revision.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<CustomPackagingRevision>> GetRevisionsAsync(Guid customPackagingId,
        CancellationToken cancellationToken = default) =>
        await context.CustomPackagingRevisions.AsNoTracking().Where(revision =>
                revision.CustomPackagingId == customPackagingId && !revision.IsDeleted)
            .OrderBy(revision => revision.RevisionNumber).ToListAsync(cancellationToken);

    public Task<bool> IsShopOwnedByUserAsync(Guid shopId, Guid userId,
        CancellationToken cancellationToken = default) =>
        (from shop in context.Shops.AsNoTracking()
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where shop.Id == shopId && profile.UserId == userId && !shop.IsDeleted && !profile.IsDeleted
         select shop.Id).AnyAsync(cancellationToken);

    public async Task<bool> CreateAsync(CustomPackaging packaging, Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            context.CustomPackagings.Add(packaging);
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = AggregateType,
                AggregateId = packaging.Id,
                EventType = "PackagingRequested",
                ToStatus = CustomPackagingStatus.Pending.ToString(),
                ActorUserId = customerId
            });
            var supplierUserId = await GetSupplierUserIdAsync(packaging.OrderId, cancellationToken);
            context.Notifications.Add(new Notification
            {
                UserId = supplierUserId,
                Type = "packaging.requested",
                Title = "Custom packaging requested",
                Content = "A customer submitted a custom packaging request for review.",
                ReferenceType = ReferenceType,
                ReferenceId = packaging.Id
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<CustomPackagingRevision?> SubmitRevisionAsync(CustomPackaging packaging,
        CustomPackagingRevision revision, Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var current = await context.CustomPackagings.FromSqlInterpolated(
                $"SELECT * FROM custom_packagings WHERE id = {packaging.Id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null || current.IsDeleted || current.Status is not (CustomPackagingStatus.Pending or
            CustomPackagingStatus.DesignReview or CustomPackagingStatus.RevisionRequested))
            return null;

        revision.RevisionNumber = await context.CustomPackagingRevisions
            .Where(item => item.CustomPackagingId == current.Id)
            .Select(item => (int?)item.RevisionNumber).MaxAsync(cancellationToken) ?? 0;
        revision.RevisionNumber++;
        current.Status = CustomPackagingStatus.DesignReview;
        current.UpdatedAt = revision.SubmittedAt;
        current.UpdatedBy = revision.SubmittedByUserId;
        context.CustomPackagingRevisions.Add(revision);
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = AggregateType,
            AggregateId = current.Id,
            EventType = "PackagingRevisionSubmitted",
            FromStatus = packaging.Status.ToString(),
            ToStatus = CustomPackagingStatus.DesignReview.ToString(),
            ActorUserId = revision.SubmittedByUserId
        });
        context.Notifications.Add(new Notification
        {
            UserId = customerId,
            Type = "packaging.revision_submitted",
            Title = "Packaging mockup ready",
            Content = $"Packaging mockup revision {revision.RevisionNumber} is ready for review.",
            ReferenceType = ReferenceType,
            ReferenceId = current.Id
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        packaging.Status = current.Status;
        packaging.UpdatedAt = current.UpdatedAt;
        packaging.UpdatedBy = current.UpdatedBy;
        return revision;
    }

    public async Task<bool> ReviewRevisionAsync(CustomPackaging packaging,
        CustomPackagingRevision revision, bool approve, string? customerNotes, Guid actorUserId,
        DateTime reviewedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var currentRevision = await context.CustomPackagingRevisions.FromSqlInterpolated(
                $"SELECT * FROM custom_packaging_revisions WHERE \"Id\" = {revision.Id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (currentRevision is null || currentRevision.IsDeleted ||
            currentRevision.Status != CustomPackagingStatus.DesignReview)
            return false;

        var currentPackaging = await context.CustomPackagings.FromSqlInterpolated(
                $"SELECT * FROM custom_packagings WHERE id = {packaging.Id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (currentPackaging is null || currentPackaging.IsDeleted ||
            currentRevision.CustomPackagingId != currentPackaging.Id)
            return false;

        var nextStatus = approve ? CustomPackagingStatus.Approved : CustomPackagingStatus.RevisionRequested;
        currentRevision.Status = nextStatus;
        currentRevision.CustomerNotes = customerNotes;
        currentRevision.ReviewedAt = reviewedAtUtc;
        currentRevision.UpdatedAt = reviewedAtUtc;
        currentRevision.UpdatedBy = actorUserId;
        currentPackaging.Status = nextStatus;
        currentPackaging.UpdatedAt = reviewedAtUtc;
        currentPackaging.UpdatedBy = actorUserId;

        if (approve)
        {
            currentPackaging.BoxDesignUrl = currentRevision.MockupUrl;
            var order = await context.Orders.FromSqlInterpolated(
                    $"SELECT * FROM orders WHERE id = {currentPackaging.OrderId} FOR UPDATE")
                .SingleAsync(cancellationToken);
            order.PackagingFee += currentRevision.DesignFee;
            order.TotalAmount += currentRevision.DesignFee;
            order.UpdatedAt = reviewedAtUtc;
            order.UpdatedBy = actorUserId;
        }

        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = AggregateType,
            AggregateId = currentPackaging.Id,
            EventType = approve ? "PackagingRevisionApproved" : "PackagingRevisionRequested",
            FromStatus = CustomPackagingStatus.DesignReview.ToString(),
            ToStatus = nextStatus.ToString(),
            ActorUserId = actorUserId,
            Reason = customerNotes
        });
        var supplierUserId = await GetSupplierUserIdAsync(currentPackaging.OrderId, cancellationToken);
        context.Notifications.Add(new Notification
        {
            UserId = supplierUserId,
            Type = approve ? "packaging.approved" : "packaging.revision_requested",
            Title = approve ? "Packaging mockup approved" : "Packaging revision requested",
            Content = approve
                ? "The customer approved the packaging mockup."
                : "The customer requested another packaging mockup revision.",
            ReferenceType = ReferenceType,
            ReferenceId = currentPackaging.Id
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        packaging.Status = currentPackaging.Status;
        packaging.BoxDesignUrl = currentPackaging.BoxDesignUrl;
        packaging.UpdatedAt = currentPackaging.UpdatedAt;
        revision.Status = currentRevision.Status;
        revision.CustomerNotes = currentRevision.CustomerNotes;
        revision.ReviewedAt = currentRevision.ReviewedAt;
        return true;
    }

    private Task<Guid> GetSupplierUserIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        (from order in context.Orders.AsNoTracking()
         join shop in context.Shops.AsNoTracking() on order.ShopId equals shop.Id
         join profile in context.SupplierProfiles.AsNoTracking() on shop.SupplierId equals profile.Id
         where order.Id == orderId
         select profile.UserId).SingleAsync(cancellationToken);
}
