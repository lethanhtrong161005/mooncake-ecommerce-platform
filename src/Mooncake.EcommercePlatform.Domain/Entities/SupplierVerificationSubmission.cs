namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Immutable business details and document set submitted for one review.</summary>
public sealed class SupplierVerificationSubmission : BaseEntity
{
    public Guid SupplierProfileId { get; set; }
    public int Revision { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SupplierVerificationStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string Source { get; set; } = "Web";
}
