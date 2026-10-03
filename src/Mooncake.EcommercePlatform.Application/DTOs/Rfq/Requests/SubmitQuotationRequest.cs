namespace Mooncake.EcommercePlatform.Application.DTOs.Rfq.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Request for a supplier to submit a quotation for an RFQ.</summary>
public record SubmitQuotationRequest
{
    [Required]
    public long RfqId { get; init; }

    [Required]
    public long SupplierId { get; init; }

    [Range(1, 365)]
    public int? LeadTimeDays { get; init; }

    [Range(0, 100)]
    public decimal ProposedDepositPercent { get; init; } = 30m;

    public DateTime? ValidUntilUtc { get; init; }

    public string? Notes { get; init; }

    [Required]
    [MinLength(1)]
    public List<SubmitQuotationItemRequest> Items { get; init; } = [];
}

public record SubmitQuotationItemRequest
{
    [Required]
    public long RfqItemId { get; init; }

    [Required]
    [Range(0, 100000000)]
    public decimal UnitPrice { get; init; }

    public string? Note { get; init; }
}
