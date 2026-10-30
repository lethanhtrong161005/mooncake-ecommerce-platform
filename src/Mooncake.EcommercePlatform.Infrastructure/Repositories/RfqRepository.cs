namespace Mooncake.EcommercePlatform.Infrastructure.Repositories;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

public sealed class RfqRepository(ApplicationDbContext context) : IRfqRepository
{
    public Task<RequestForQuotation?> GetRfqAsync(Guid rfqId, CancellationToken cancellationToken = default) =>
        context.RequestForQuotations.FirstOrDefaultAsync(rfq => rfq.Id == rfqId && !rfq.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<RequestForQuotation>> GetCustomerRfqsAsync(Guid customerUserId, CancellationToken cancellationToken = default) =>
        await context.RequestForQuotations.AsNoTracking().Where(rfq => rfq.CustomerId == customerUserId && !rfq.IsDeleted)
            .OrderByDescending(rfq => rfq.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RequestForQuotation>> GetSupplierInvitedRfqsAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default) =>
        await (from invitation in context.RfqInvitations.AsNoTracking()
               join profile in context.SupplierProfiles.AsNoTracking() on invitation.SupplierId equals profile.Id
               join rfq in context.RequestForQuotations.AsNoTracking() on invitation.RfqId equals rfq.Id
               where profile.UserId == supplierUserId && !profile.IsDeleted && !invitation.IsDeleted && !rfq.IsDeleted
               orderby rfq.BidDeadline ascending
               select rfq).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SupplierProfile>> GetSuggestedSuppliersAsync(Guid rfqId,
        CancellationToken cancellationToken = default) =>
        await (from profile in context.SupplierProfiles.AsNoTracking()
               join shop in context.Shops.AsNoTracking() on profile.Id equals shop.SupplierId
               join user in context.Users.AsNoTracking() on profile.UserId equals user.Id
               where profile.VerificationStatus == SupplierVerificationStatus.Verified &&
                     profile.ReputationScore >= 80 && shop.Status == ShopStatus.Active && user.IsActive &&
                     !profile.IsDeleted && !shop.IsDeleted && !user.IsDeleted &&
                     !context.RfqInvitations.Any(invitation => invitation.RfqId == rfqId &&
                         invitation.SupplierId == profile.Id && !invitation.IsDeleted)
               orderby profile.ReputationScore descending, profile.AvgRating descending
               select profile).Take(10).ToListAsync(cancellationToken);

    public Task<Guid?> GetVerifiedSupplierProfileIdAsync(Guid supplierUserId, CancellationToken cancellationToken = default) =>
        context.SupplierProfiles.AsNoTracking().Where(profile => profile.UserId == supplierUserId && !profile.IsDeleted
                && profile.VerificationStatus == SupplierVerificationStatus.Verified)
            .Select(profile => (Guid?)profile.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<RfqItem>> GetRfqItemsAsync(Guid rfqId, CancellationToken cancellationToken = default) =>
        await context.RfqItems.AsNoTracking().Where(item => item.RfqId == rfqId && !item.IsDeleted)
            .OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RfqInvitation>> GetInvitationsAsync(Guid rfqId, CancellationToken cancellationToken = default) =>
        await context.RfqInvitations.AsNoTracking().Where(invitation => invitation.RfqId == rfqId && !invitation.IsDeleted)
            .ToListAsync(cancellationToken);

    public Task<RfqInvitation?> GetInvitationAsync(Guid rfqId, Guid supplierProfileId, CancellationToken cancellationToken = default) =>
        context.RfqInvitations.FirstOrDefaultAsync(invitation => invitation.RfqId == rfqId
            && invitation.SupplierId == supplierProfileId && !invitation.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Bid>> GetBidsAsync(Guid rfqId, Guid? supplierProfileId = null, CancellationToken cancellationToken = default) =>
        await context.Bids.AsNoTracking().Where(bid => bid.RfqId == rfqId && !bid.IsDeleted
                && (!supplierProfileId.HasValue || bid.SupplierId == supplierProfileId.Value))
            .OrderBy(bid => bid.SubmittedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BidItem>> GetBidItemsAsync(Guid bidId, CancellationToken cancellationToken = default) =>
        await context.BidItems.AsNoTracking().Where(item => item.BidId == bidId && !item.IsDeleted)
            .OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);

    public Task<Bid?> GetBidAsync(Guid bidId, CancellationToken cancellationToken = default) =>
        context.Bids.FirstOrDefaultAsync(bid => bid.Id == bidId && !bid.IsDeleted, cancellationToken);

    public Task<bool> HasAnyBidAsync(Guid rfqId, CancellationToken cancellationToken = default) =>
        context.Bids.AnyAsync(bid => bid.RfqId == rfqId && !bid.IsDeleted, cancellationToken);

    public Task<bool> IsActiveSupplierAsync(Guid supplierProfileId, CancellationToken cancellationToken = default) =>
        (from profile in context.SupplierProfiles
         join shop in context.Shops on profile.Id equals shop.SupplierId
         join user in context.Users on profile.UserId equals user.Id
         where profile.Id == supplierProfileId && profile.VerificationStatus == SupplierVerificationStatus.Verified
               && shop.Status == ShopStatus.Active && !profile.IsDeleted && !shop.IsDeleted && user.IsActive && !user.IsDeleted
         select profile.Id).AnyAsync(cancellationToken);

    public async Task<bool> CreateRfqAsync(RequestForQuotation rfq, IReadOnlyList<RfqItem> items, CancellationToken cancellationToken = default)
    {
        rfq.Id = rfq.Id == Guid.Empty ? Guid.NewGuid() : rfq.Id;
        foreach (var item in items) { item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id; item.RfqId = rfq.Id; }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.RequestForQuotations.Add(rfq);
        context.RfqItems.AddRange(items);
        context.Notifications.Add(new Notification
        {
            UserId = rfq.CustomerId, Type = "rfq.created", Title = "RFQ created",
            Content = "Your request for quotation is ready to publish.", ReferenceType = "rfq", ReferenceId = rfq.Id
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task SaveRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default)
    {
        var entry = context.Entry(rfq);
        var previousStatus = entry.Property(item => item.Status).OriginalValue;
        rfq.UpdatedAt = DateTime.UtcNow;
        if (previousStatus != rfq.Status)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Rfq", AggregateId = rfq.Id, EventType = "RfqStatusChanged",
                FromStatus = previousStatus.ToString(), ToStatus = rfq.Status.ToString(), ActorUserId = rfq.UpdatedBy
            });
            context.Notifications.Add(new Notification
            {
                UserId = rfq.CustomerId, Type = "rfq.status_changed", Title = "RFQ status updated",
                Content = $"Your RFQ status changed to {rfq.Status}.", ReferenceType = "rfq", ReferenceId = rfq.Id
            });
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddInvitationsAsync(IReadOnlyList<RfqInvitation> invitations, CancellationToken cancellationToken = default)
    {
        foreach (var invitation in invitations) if (invitation.Id == Guid.Empty) invitation.Id = Guid.NewGuid();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.RfqInvitations.AddRange(invitations);
        var profiles = await context.SupplierProfiles.Where(profile => invitations.Select(invitation => invitation.SupplierId).Contains(profile.Id))
            .ToListAsync(cancellationToken);
        var rfqId = invitations[0].RfqId;
        foreach (var profile in profiles)
            context.Notifications.Add(new Notification
            {
                UserId = profile.UserId, Type = "rfq.invited", Title = "New RFQ invitation",
                Content = "You have been invited to submit a quotation.", ReferenceType = "rfq", ReferenceId = rfqId
            });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveInvitationAsync(RfqInvitation invitation, CancellationToken cancellationToken = default)
    {
        var previousStatus = context.Entry(invitation).Property(item => item.Status).OriginalValue;
        invitation.UpdatedAt = DateTime.UtcNow;
        if (previousStatus != invitation.Status)
        {
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "RfqInvitation", AggregateId = invitation.Id, EventType = "InvitationStatusChanged",
                FromStatus = previousStatus.ToString(), ToStatus = invitation.Status.ToString(), ActorUserId = invitation.UpdatedBy,
                Reason = invitation.DeclineReason
            });
            if (invitation.Status == RfqInvitationStatus.Declined)
            {
                var customerId = await context.RequestForQuotations.Where(rfq => rfq.Id == invitation.RfqId)
                    .Select(rfq => rfq.CustomerId).FirstOrDefaultAsync(cancellationToken);
                context.Notifications.Add(new Notification
                {
                    UserId = customerId, Type = "rfq.invitation_declined", Title = "Supplier declined the invitation",
                    Content = "A supplier declined your RFQ invitation.", ReferenceType = "rfq", ReferenceId = invitation.RfqId
                });
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CreateBidAsync(Bid bid, IReadOnlyList<BidItem> items, CancellationToken cancellationToken = default)
    {
        bid.Id = bid.Id == Guid.Empty ? Guid.NewGuid() : bid.Id;
        foreach (var item in items) { item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id; item.BidId = bid.Id; }
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var eligibility = await Mooncake.EcommercePlatform.Infrastructure.Helpers.SupplierEligibilityLockHelper.LockAsync(
            context, bid.SupplierId, [], cancellationToken);
        if (!Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanSell(eligibility.Owner, eligibility.Profile))
            return false;
        if (!await Mooncake.EcommercePlatform.Infrastructure.Helpers.ShopTradingLockHelper.CanTradeAsync(context, bid.SupplierId, cancellationToken))
            return false;
        var existingBid = await context.Bids.FirstOrDefaultAsync(existing => existing.RfqId == bid.RfqId && existing.SupplierId == bid.SupplierId &&
                !existing.IsDeleted, cancellationToken);
        if (existingBid is not null && existingBid.Status != BidStatus.Submitted)
            return false;
        if (existingBid is null)
        {
            context.Bids.Add(bid);
        }
        else
        {
            existingBid.TotalPrice = bid.TotalPrice;
            existingBid.EstimatedDeliveryDays = bid.EstimatedDeliveryDays;
            existingBid.ValidityDays = bid.ValidityDays;
            existingBid.Notes = bid.Notes;
            existingBid.UpdatedAt = DateTime.UtcNow;
            existingBid.UpdatedBy = bid.CreatedBy;
            var oldItems = await context.BidItems.Where(item => item.BidId == existingBid.Id).ToListAsync(cancellationToken);
            context.BidItems.RemoveRange(oldItems);
            foreach (var item in items) item.BidId = existingBid.Id;
        }
        context.BidItems.AddRange(items);
        var invitation = await context.RfqInvitations.FirstOrDefaultAsync(item => item.RfqId == bid.RfqId
            && item.SupplierId == bid.SupplierId && !item.IsDeleted, cancellationToken);
        if (invitation is null) return false;
        invitation.Status = RfqInvitationStatus.BidSubmitted;
        invitation.RespondedAt = bid.SubmittedAt;
        invitation.UpdatedAt = bid.SubmittedAt;
        var customerId = await context.RequestForQuotations.Where(rfq => rfq.Id == bid.RfqId).Select(rfq => rfq.CustomerId).FirstAsync(cancellationToken);
        context.Notifications.Add(new Notification
        {
            UserId = customerId, Type = "bid.submitted", Title = "New quotation received",
            Content = "A supplier has submitted a quotation for your RFQ.", ReferenceType = "rfq", ReferenceId = bid.RfqId
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<Contract?> AwardBidAsync(Guid rfqId, Guid bidId, string? selectionNote, Guid customerUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var selectedSupplier = await context.Bids.AsNoTracking().Where(x => x.Id == bidId && x.RfqId == rfqId && !x.IsDeleted)
            .Select(x => (Guid?)x.SupplierId).SingleOrDefaultAsync(cancellationToken);
        if (selectedSupplier == null) return null;
        var eligibility = await Mooncake.EcommercePlatform.Infrastructure.Helpers.SupplierEligibilityLockHelper.LockAsync(
            context, selectedSupplier.Value, [customerUserId], cancellationToken);
        if (!Mooncake.EcommercePlatform.Application.Common.Helpers.SupplierVerificationHelper.CanSell(eligibility.Owner, eligibility.Profile))
            return null;
        if (!await Mooncake.EcommercePlatform.Infrastructure.Helpers.ShopTradingLockHelper.CanTradeAsync(context, selectedSupplier.Value, cancellationToken))
            return null;
        var rfq = await context.RequestForQuotations.FromSqlInterpolated($"SELECT * FROM request_for_quotations WHERE id = {rfqId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (rfq is null || rfq.IsDeleted || rfq.CustomerId != customerUserId) return null;
        var existingContract = await context.Contracts.FirstOrDefaultAsync(contract => contract.BidId == bidId && !contract.IsDeleted, cancellationToken);
        if (rfq.Status == RfqStatus.Awarded && existingContract is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingContract;
        }
        if (rfq.Status is not (RfqStatus.Open or RfqStatus.Closed)) return null;
        if (rfq.Status == RfqStatus.Open && rfq.BidDeadline > DateTime.UtcNow
            && await context.RfqInvitations.AnyAsync(invitation => invitation.RfqId == rfqId && !invitation.IsDeleted
                && (invitation.Status == RfqInvitationStatus.Invited || invitation.Status == RfqInvitationStatus.Viewed), cancellationToken)) return null;
        var bid = await context.Bids.FirstOrDefaultAsync(item => item.Id == bidId && item.RfqId == rfqId && !item.IsDeleted, cancellationToken);
        if (bid is null || bid.Status != BidStatus.Submitted || bid.SubmittedAt.AddDays(bid.ValidityDays) <= DateTime.UtcNow) return null;
        var supplier = await context.SupplierProfiles.FirstOrDefaultAsync(profile => profile.Id == bid.SupplierId && !profile.IsDeleted, cancellationToken);
        if (supplier is null) return null;
        var bidLines = await (from bidItem in context.BidItems.AsNoTracking()
                              join rfqItem in context.RfqItems.AsNoTracking() on bidItem.RfqItemId equals rfqItem.Id
                              where bidItem.BidId == bid.Id && !bidItem.IsDeleted && !rfqItem.IsDeleted
                              orderby rfqItem.CreatedAt
                              select new
                              {
                                  rfqItem.ProductName, rfqItem.Description, rfqItem.Quantity, rfqItem.Unit,
                                  rfqItem.Specifications, rfqItem.PackagingRequirements,
                                  bidItem.UnitPrice, bidItem.Subtotal, bidItem.Notes
                              }).ToListAsync(cancellationToken);

        bid.Status = BidStatus.Accepted;
        bid.AcceptedAt = DateTime.UtcNow;
        bid.UpdatedAt = DateTime.UtcNow;
        rfq.Status = RfqStatus.Awarded;
        rfq.AwardedBidId = bid.Id;
        rfq.AwardedAt = DateTime.UtcNow;
        rfq.SelectionNote = selectionNote;
        rfq.UpdatedAt = DateTime.UtcNow;
        rfq.UpdatedBy = customerUserId;
        var otherBids = await context.Bids.Where(item => item.RfqId == rfqId && item.Id != bidId && item.Status == BidStatus.Submitted && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var otherBid in otherBids) { otherBid.Status = BidStatus.Cancelled; otherBid.UpdatedAt = DateTime.UtcNow; }

        var contract = new Contract
        {
            Id = Guid.NewGuid(), RfqId = rfqId, BidId = bidId, CustomerId = customerUserId, SupplierId = supplier.Id,
            ContractNumber = $"HD-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..19].ToUpperInvariant(),
            Title = rfq.Title, TotalValue = bid.TotalPrice, DeliveryDeadline = rfq.DeliveryDateRequired,
            TermsAndConditions = JsonSerializer.Serialize(new
            {
                rfq.RfqNumber, rfq.Title, rfq.Description, rfq.DeliveryAddress, rfq.DeliveryDateRequired,
                bid.BidNumber, supplier.CompanyName, SupplierTaxCode = supplier.TaxCode,
                Items = bidLines
            }),
            Status = ContractStatus.Draft, CreatedBy = customerUserId
        };
        context.Contracts.Add(contract);
        context.Notifications.Add(new Notification
        {
            UserId = supplier.UserId, Type = "bid.accepted", Title = "Your quotation was selected",
            Content = "The customer selected your quotation. A draft contract is ready for review.", ReferenceType = "contract", ReferenceId = contract.Id
        });
        context.Notifications.Add(new Notification
        {
            UserId = customerUserId, Type = "contract.draft_ready", Title = "Draft contract created",
            Content = "A draft contract was created from the selected quotation.", ReferenceType = "contract", ReferenceId = contract.Id
        });
        foreach (var otherBid in otherBids)
        {
            var losingSupplier = await context.SupplierProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.Id == otherBid.SupplierId, cancellationToken);
            if (losingSupplier is not null)
                context.Notifications.Add(new Notification
                {
                    UserId = losingSupplier.UserId, Type = "bid.not_selected", Title = "Quotation not selected",
                    Content = "The customer selected another quotation for this RFQ.", ReferenceType = "rfq", ReferenceId = rfqId
                });
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return contract;
    }

    public async Task<bool> CancelBidAsync(Guid rfqId, Guid bidId, Guid customerUserId, CancellationToken cancellationToken = default)
    {
        var bid = await context.Bids.FirstOrDefaultAsync(item => item.Id == bidId && item.RfqId == rfqId && !item.IsDeleted, cancellationToken);
        var rfq = await context.RequestForQuotations.FirstOrDefaultAsync(item => item.Id == rfqId && !item.IsDeleted, cancellationToken);
        if (bid is null || rfq is null || rfq.CustomerId != customerUserId || bid.Status != BidStatus.Submitted) return false;
        bid.Status = BidStatus.Cancelled;
        bid.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelRfqAsync(Guid rfqId, Guid customerUserId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var rfq = await context.RequestForQuotations.FromSqlInterpolated($"SELECT * FROM request_for_quotations WHERE id = {rfqId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        if (rfq is null || rfq.IsDeleted || rfq.CustomerId != customerUserId || rfq.Status is RfqStatus.Awarded or RfqStatus.Cancelled)
            return false;

        var previousStatus = rfq.Status;
        var now = DateTime.UtcNow;
        rfq.Status = RfqStatus.Cancelled;
        rfq.CancelReason = reason;
        rfq.UpdatedAt = now;
        rfq.UpdatedBy = customerUserId;
        var submittedBids = await context.Bids.Where(bid => bid.RfqId == rfqId && bid.Status == BidStatus.Submitted && !bid.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var bid in submittedBids)
        {
            bid.Status = BidStatus.Cancelled;
            bid.CancelReason = "The customer cancelled the RFQ.";
            bid.UpdatedAt = now;
        }
        context.WorkflowEvents.Add(new WorkflowEvent
        {
            AggregateType = "Rfq", AggregateId = rfq.Id, EventType = "RfqStatusChanged",
            FromStatus = previousStatus.ToString(), ToStatus = RfqStatus.Cancelled.ToString(), ActorUserId = customerUserId, Reason = reason
        });
        context.Notifications.Add(new Notification
        {
            UserId = customerUserId, Type = "rfq.status_changed", Title = "RFQ cancelled",
            Content = "Your request for quotation was cancelled.", ReferenceType = "rfq", ReferenceId = rfq.Id
        });
        var supplierUserIds = await (from invitation in context.RfqInvitations.AsNoTracking()
                                     join profile in context.SupplierProfiles.AsNoTracking() on invitation.SupplierId equals profile.Id
                                     where invitation.RfqId == rfqId && !invitation.IsDeleted
                                     select profile.UserId).Distinct().ToListAsync(cancellationToken);
        context.Notifications.AddRange(supplierUserIds.Select(userId => new Notification
        {
            UserId = userId, Type = "rfq.cancelled", Title = "RFQ cancelled",
            Content = "A customer cancelled an RFQ you were invited to.", ReferenceType = "rfq", ReferenceId = rfq.Id
        }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int> CloseExpiredRfqsAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var expiredRfqs = await context.RequestForQuotations.FromSqlInterpolated($"SELECT * FROM request_for_quotations WHERE status = 'Open'::rfq_status AND bid_deadline <= {now} AND is_deleted = FALSE FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
        foreach (var rfq in expiredRfqs)
        {
            rfq.Status = RfqStatus.Closed;
            rfq.UpdatedAt = now;
            context.WorkflowEvents.Add(new WorkflowEvent
            {
                AggregateType = "Rfq", AggregateId = rfq.Id, EventType = "RfqStatusChanged",
                FromStatus = RfqStatus.Open.ToString(), ToStatus = RfqStatus.Closed.ToString(), Reason = "Bid deadline elapsed."
            });
            context.Notifications.Add(new Notification
            {
                UserId = rfq.CustomerId, Type = "rfq.closed", Title = "RFQ deadline reached",
                Content = "The bidding deadline has passed. You can review and select a submitted bid.",
                ReferenceType = "rfq", ReferenceId = rfq.Id
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return expiredRfqs.Count;
    }
}
