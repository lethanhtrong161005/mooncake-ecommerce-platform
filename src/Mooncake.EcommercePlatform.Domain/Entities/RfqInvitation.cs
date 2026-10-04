namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the RfqInvitation domain entity.</summary>
public class RfqInvitation : BaseEntity
{
    public Guid RfqId { get; set; }

    public Guid SupplierId { get; set; }

    public DateTime InvitedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public RfqInvitationStatus Status { get; set; } = RfqInvitationStatus.Invited;
}
