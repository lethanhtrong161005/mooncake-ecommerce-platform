using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class PriceNegotiation : BaseEntity
{
    public long QuotationId { get; set; }
    public int RoundNo { get; set; }
    public NegotiationProposedBy ProposedBy { get; set; }
    public decimal ProposedAmount { get; set; }
    public string? Message { get; set; }
    public NegotiationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
