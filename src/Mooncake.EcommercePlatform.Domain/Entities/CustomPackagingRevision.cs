namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents a version of a custom packaging design submitted for review.</summary>
public class CustomPackagingRevision : BaseEntity
{
    public Guid CustomPackagingId { get; set; }

    public int RevisionNumber { get; set; }

    public string? MockupUrl { get; set; }

    public string? CustomerNotes { get; set; }

    public string? SupplierNotes { get; set; }

    public decimal DesignFee { get; set; }

    public CustomPackagingStatus Status { get; set; } = CustomPackagingStatus.DesignReview;

    public Guid? SubmittedByUserId { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
