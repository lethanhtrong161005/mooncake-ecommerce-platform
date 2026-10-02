namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Contract for B2B RFQ, Quotation, and Negotiation operations.</summary>
public interface IRfqService
{
    Task<RfqResponse> CreateRfqAsync(CreateRfqRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<RfqResponse>> GetRfqsAsync(long? customerId = null, RfqStatus? status = null, CancellationToken cancellationToken = default);
    Task<RfqResponse?> GetRfqByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> InviteSupplierAsync(long rfqId, InviteSupplierRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<RfqResponse>> GetSupplierInvitationsAsync(long supplierId, CancellationToken cancellationToken = default);

    Task<QuotationResponse> SubmitQuotationAsync(SubmitQuotationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<QuotationResponse>> GetQuotationsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<IEnumerable<QuotationResponse>> GetQuotationsBySupplierIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<QuotationResponse?> GetQuotationByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PriceNegotiationResponse> CreateNegotiationAsync(long quotationId, CreateNegotiationRequest request, CancellationToken cancellationToken = default);
    Task<PriceNegotiationResponse> RespondNegotiationAsync(long negotiationId, RespondNegotiationRequest request, CancellationToken cancellationToken = default);
    Task<QuotationResponse> AcceptQuotationAsync(long quotationId, CancellationToken cancellationToken = default);

    Task<RfqComparisonResponse> CompareQuotationsAsync(long rfqId, CancellationToken cancellationToken = default);
    Task CheckExpirationsAsync(CancellationToken cancellationToken = default);
}
