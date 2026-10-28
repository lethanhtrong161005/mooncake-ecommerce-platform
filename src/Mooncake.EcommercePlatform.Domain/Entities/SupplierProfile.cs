namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Represents the SupplierProfile domain entity.</summary>
public class SupplierProfile : BaseEntity
{
    public long Version { get; set; }
    public Guid? CurrentSubmissionId { get; set; }
    public Guid? LastApprovedSubmissionId { get; set; }
    public DateTime? LastSubmittedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public Guid UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Description { get; set; }

    public decimal ReputationScore { get; set; }

    public int TotalContracts { get; set; }

    public int ContractsOnTime { get; set; }

    public int ContractsLate { get; set; }

    public int ContractsBreached { get; set; }

    public decimal? AvgRating { get; set; }

    public SupplierVerificationStatus VerificationStatus { get; set; } = SupplierVerificationStatus.Pending;

    public DateTime? VerifiedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }
}
