using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;
using System;

namespace Mooncake.EcommercePlatform.Domain.Entities;

public class RfqInvitation : BaseEntity
{
    public long RfqId { get; set; }
    public long SupplierId { get; set; }
    public RfqInvitationStatus Status { get; set; }
    public DateTime InvitedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }

    public RequestForQuotation RequestForQuotation { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
