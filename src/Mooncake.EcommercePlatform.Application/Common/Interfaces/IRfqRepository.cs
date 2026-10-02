namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Data access contract for RFQ, quotations, invitations, and negotiations.</summary>
public interface IRfqRepository
{
    Task<IEnumerable<RequestForQuotation>> GetRfqsAsync(long? customerId = null, RfqStatus? status = null, CancellationToken cancellationToken = default);
    Task<RequestForQuotation?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RequestForQuotation> CreateRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default);
    Task<RequestForQuotation> UpdateRfqAsync(RequestForQuotation rfq, CancellationToken cancellationToken = default);

    Task<RfqInvitation> AddInvitationAsync(RfqInvitation invitation, CancellationToken cancellationToken = default);
    Task<IEnumerable<RfqInvitation>> GetInvitationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Quotation>> GetQuotationsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quotation>> GetQuotationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<Quotation?> GetQuotationByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Quotation> CreateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default);
    Task<Quotation> UpdateQuotationAsync(Quotation quotation, CancellationToken cancellationToken = default);

    Task<PriceNegotiation> AddNegotiationAsync(PriceNegotiation negotiation, CancellationToken cancellationToken = default);
    Task<PriceNegotiation?> GetNegotiationByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PriceNegotiation> UpdateNegotiationAsync(PriceNegotiation negotiation, CancellationToken cancellationToken = default);

    Task CheckAndExpireRfqsAndQuotationsAsync(CancellationToken cancellationToken = default);
}
