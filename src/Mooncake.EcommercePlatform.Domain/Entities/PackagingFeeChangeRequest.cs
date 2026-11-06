namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Records an auditable packaging fee adjustment proposed in a mockup revision without mutating settled payment obligations.</summary>
public sealed class PackagingFeeChangeRequest : BaseEntity
{
    public Guid OrderId { get; set; }

    public Guid CustomPackagingId { get; set; }

    public Guid RevisionId { get; set; }

    public decimal OldPackagingFee { get; set; }

    public decimal DeltaAmount { get; set; }

    public decimal NewPackagingFee { get; set; }

    public decimal OldOrderTotal { get; set; }

    public decimal NewOrderTotal { get; set; }

    public long OrderVersionSnapshot { get; set; }

    public string Status { get; set; } = "PendingCustomerApproval";

    public string? CustomerNotes { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? GeneratedObligationId { get; set; }
}

