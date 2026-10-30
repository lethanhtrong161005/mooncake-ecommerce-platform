namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IRfqRepository
{
    Task<RequestForQuotation?> GetRfqAsync(Guid rfqId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestForQuotation>> GetCustomerRfqsAsync(Guid customerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestForQuotation>> GetSupplierInvitedRfqsAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierProfile>> GetSuggestedSuppliersAsync(Guid rfqId, CancellationToken cancellationToken = default);
    Task<Guid?> GetVerifiedSupplierProfileIdAsync(Guid supplierUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqItem>> GetRfqItemsAsync(Guid rfqId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqInvitation>> GetInvitationsAsync(Guid rfqId, CancellationToken cancellationToken = default);
    Task<RfqInvitation?> GetInvitationAsync(Guid rfqId, Guid supplierProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bid>> GetBidsAsync(Guid rfqId, Guid? supplierProfileId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BidItem>> GetBidItemsAsync(Guid bidId, CancellationToken cancellationToken = default);
    Task<Bid?> GetBidAsync(Guid bidId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyBidAsync(Guid rfqId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveSupplierAsync(Guid supplierProfileId, CancellationToken cancellationToken = default);
    Task<bool> CreateRfqAsync(RequestForQuotation rfq, IReadOnlyList<RfqItem> items, CancellationToken cancellationToken = default);
    Task SaveRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default);
    Task AddInvitationsAsync(IReadOnlyList<RfqInvitation> invitations, CancellationToken cancellationToken = default);
    Task SaveInvitationAsync(RfqInvitation invitation, CancellationToken cancellationToken = default);
    Task<bool> CreateBidAsync(Bid bid, IReadOnlyList<BidItem> items, CancellationToken cancellationToken = default);
    Task<Contract?> AwardBidAsync(Guid rfqId, Guid bidId, string? selectionNote, Guid customerUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelBidAsync(Guid rfqId, Guid bidId, Guid customerUserId, CancellationToken cancellationToken = default);
    Task<bool> CancelRfqAsync(Guid rfqId, Guid customerUserId, string reason, CancellationToken cancellationToken = default);
    Task<int> CloseExpiredRfqsAsync(CancellationToken cancellationToken = default);
}
