using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class Quotation : BaseEntity
{
    public long RfqId { get; set; }
    public long SupplierId { get; set; }
    public QuotationStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal ProposedDepositPercent { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public RequestForQuotation RequestForQuotation { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<QuotationItem> QuotationItems { get; set; } = [];
    public ICollection<PriceNegotiation> PriceNegotiations { get; set; } = [];
    public Contract? Contract { get; set; }
}
