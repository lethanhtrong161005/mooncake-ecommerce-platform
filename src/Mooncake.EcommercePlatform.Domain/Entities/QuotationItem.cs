using Mooncake.EcommercePlatform.Domain.Common;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class QuotationItem : BaseEntity
{
    public long QuotationId { get; set; }
    public long RfqItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public RfqItem RfqItem { get; set; } = null!;
}
