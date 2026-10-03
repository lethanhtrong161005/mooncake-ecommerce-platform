namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Contract for mapping RFQ and Quotation domain entities to response DTOs.</summary>
public interface IRfqHelper
{
    RfqResponse ToResponse(RequestForQuotation rfq);
    QuotationResponse ToResponse(Quotation quotation);
    RfqComparisonResponse ToComparisonResponse(RequestForQuotation rfq, IEnumerable<Quotation> quotations);
}
