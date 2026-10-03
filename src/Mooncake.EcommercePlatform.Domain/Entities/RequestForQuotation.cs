using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class RequestForQuotation : BaseEntity
{
    public long CustomerId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public RfqVisibility Visibility { get; set; }
    public RfqStatus Status { get; set; }
    public DateTime? QuoteDeadline { get; set; }
    public DateOnly? RequiredDeliveryDate { get; set; }
    public string DeliveryAddress { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<RfqItem> RfqItems { get; set; } = [];
    public ICollection<RfqInvitation> RfqInvitations { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
}
