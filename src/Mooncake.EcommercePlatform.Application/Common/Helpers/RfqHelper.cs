namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Rfq.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps RFQ and Quotation domain entities to response DTOs.</summary>
public class RfqHelper : IRfqHelper
{
    public RfqResponse ToResponse(RequestForQuotation rfq)
    {
        var items = rfq.RfqItems?.Select(i => new RfqItemResponse(
            i.Id,
            i.RfqId,
            i.ProductId,
            i.Product?.Name,
            i.ItemName,
            i.Specification,
            i.Quantity,
            i.CustomPackagingId,
            i.CustomPackaging?.Name,
            i.Note
        )).ToList() ?? [];

        var customerName = rfq.Customer?.CompanyName
            ?? rfq.Customer?.User?.FullName
            ?? $"Customer #{rfq.CustomerId}";

        return new RfqResponse(
            rfq.Id,
            rfq.CustomerId,
            customerName,
            rfq.Title,
            rfq.Description,
            rfq.Visibility,
            rfq.Status,
            rfq.QuoteDeadline,
            rfq.RequiredDeliveryDate,
            rfq.DeliveryAddress,
            rfq.CreatedAtUtc,
            rfq.UpdatedAtUtc,
            items,
            rfq.Quotations?.Count ?? 0
        );
    }

    public QuotationResponse ToResponse(Quotation quotation)
    {
        var items = quotation.QuotationItems?.Select(qi => new QuotationItemResponse(
            qi.Id,
            qi.QuotationId,
            qi.RfqItemId,
            qi.RfqItem?.ItemName ?? $"Item #{qi.RfqItemId}",
            qi.Quantity,
            qi.UnitPrice,
            qi.LineTotal,
            qi.Note
        )).ToList() ?? [];

        var negotiations = quotation.PriceNegotiations?
            .OrderBy(n => n.RoundNo)
            .Select(n => new PriceNegotiationResponse(
                n.Id,
                n.QuotationId,
                n.RoundNo,
                n.ProposedBy,
                n.ProposedAmount,
                n.Message,
                n.Status,
                n.CreatedAtUtc
            )).ToList() ?? [];

        var supplierName = quotation.Supplier?.BusinessName
            ?? quotation.Supplier?.User?.FullName
            ?? $"Supplier #{quotation.SupplierId}";

        var reputationScore = quotation.Supplier?.ReputationScore ?? 100;

        return new QuotationResponse(
            quotation.Id,
            quotation.RfqId,
            quotation.SupplierId,
            supplierName,
            reputationScore,
            quotation.Status,
            quotation.TotalAmount,
            quotation.LeadTimeDays,
            quotation.ProposedDepositPercent,
            quotation.ValidUntilUtc,
            quotation.Notes,
            quotation.CreatedAtUtc,
            quotation.UpdatedAtUtc,
            items,
            negotiations
        );
    }

    public RfqComparisonResponse ToComparisonResponse(RequestForQuotation rfq, IEnumerable<Quotation> quotations)
    {
        var rfqResponse = ToResponse(rfq);
        var quotationResponses = quotations.Select(ToResponse).ToList();

        var lowestPriceQuote = quotationResponses
            .OrderBy(q => q.TotalAmount)
            .FirstOrDefault();

        var shortestLeadQuote = quotationResponses
            .Where(q => q.LeadTimeDays.HasValue)
            .OrderBy(q => q.LeadTimeDays!.Value)
            .FirstOrDefault();

        return new RfqComparisonResponse(
            rfq.Id,
            rfq.Title,
            rfqResponse.Items,
            quotationResponses,
            lowestPriceQuote?.Id,
            lowestPriceQuote?.TotalAmount,
            shortestLeadQuote?.Id,
            shortestLeadQuote?.LeadTimeDays
        );
    }
}
