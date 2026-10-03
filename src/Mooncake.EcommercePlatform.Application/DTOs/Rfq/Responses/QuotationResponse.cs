namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Priced line item in a quotation.</summary>
public record QuotationItemResponse(
    long Id,
    long QuotationId,
    long RfqItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Note
);

/// <summary>A negotiation round between customer and supplier.</summary>
public record PriceNegotiationResponse(
    long Id,
    long QuotationId,
    int RoundNo,
    NegotiationProposedBy ProposedBy,
    decimal ProposedAmount,
    string? Message,
    NegotiationStatus Status,
    DateTime CreatedAtUtc
);

/// <summary>Quotation projection returned to callers.</summary>
public record QuotationResponse(
    long Id,
    long RfqId,
    long SupplierId,
    string SupplierBusinessName,
    int SupplierReputationScore,
    QuotationStatus Status,
    decimal TotalAmount,
    int? LeadTimeDays,
    decimal ProposedDepositPercent,
    DateTime? ValidUntilUtc,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<QuotationItemResponse> Items,
    List<PriceNegotiationResponse> Negotiations
);
