using Mooncake.EcommercePlatform.Domain.Common;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class RfqItem : BaseEntity
{
    public long RfqId { get; set; }
    public long? ProductId { get; set; }
    public string ItemName { get; set; } = null!;
    public string? Specification { get; set; }
    public int Quantity { get; set; }
    public long? CustomPackagingId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public RequestForQuotation RequestForQuotation { get; set; } = null!;
    public Product? Product { get; set; }
    public CustomPackaging? CustomPackaging { get; set; }
    public ICollection<QuotationItem> QuotationItems { get; set; } = [];
}
