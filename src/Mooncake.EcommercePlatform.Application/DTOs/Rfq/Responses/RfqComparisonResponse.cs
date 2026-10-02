namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;

/// <summary>Side-by-side comparison of all quotations received for an RFQ.</summary>
public record RfqComparisonResponse(
    long RfqId,
    string RfqTitle,
    List<RfqItemResponse> RequestedItems,
    List<QuotationResponse> Quotations,
    long? LowestPriceQuotationId,
    decimal? LowestPriceAmount,
    long? ShortestLeadTimeQuotationId,
    int? ShortestLeadTimeDays
);
