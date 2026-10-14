namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Rfqs.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

public interface IRfqService
{
    Task<RfqResponse> CreateAsync(Guid customerUserId, CreateRfqRequest request, CancellationToken cancellationToken = default);
    Task<RfqResponse> PublishAsync(Guid customerUserId, Guid rfqId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid customerUserId, Guid rfqId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqResponse>> GetMineAsync(Guid customerUserId, CancellationToken cancellationToken = default);
    Task<RfqResponse> GetAsync(Guid userId, bool isSupplier, Guid rfqId, CancellationToken cancellationToken = default);
    Task<RfqResponse> InviteAsync(Guid customerUserId, Guid rfqId, InviteSuppliersRequest request, CancellationToken cancellationToken = default);
    Task DeclineAsync(Guid supplierUserId, Guid rfqId, DeclineInvitationRequest request, CancellationToken cancellationToken = default);
    Task<BidResponse> SubmitBidAsync(Guid supplierUserId, Guid rfqId, SubmitBidRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BidResponse>> CompareBidsAsync(Guid customerUserId, Guid rfqId, CancellationToken cancellationToken = default);
    Task<BidResponse> CancelBidAsync(Guid customerUserId, Guid rfqId, Guid bidId, CancellationToken cancellationToken = default);
    Task<Contract> AwardBidAsync(Guid customerUserId, Guid rfqId, Guid bidId, AwardBidRequest request, CancellationToken cancellationToken = default);
}
