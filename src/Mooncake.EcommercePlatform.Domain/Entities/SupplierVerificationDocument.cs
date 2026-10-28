namespace Mooncake.EcommercePlatform.Domain.Entities;

using Mooncake.EcommercePlatform.Domain.Common;

/// <summary>Represents a document submitted for supplier verification.</summary>
public class SupplierVerificationDocument : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public Guid? SubmissionId { get; set; }
    public string UploadStatus { get; set; } = "Uploading";
    public Guid UploadOperationId { get; set; }
    public string? UploadRequestHash { get; set; }
    public string? Sha256 { get; set; }
    public string? ProviderPublicId { get; set; }
    public string? ProviderResourceType { get; set; }
    public string? ProviderDeliveryType { get; set; }
    public DateTime? ValidationCompletedAtUtc { get; set; }
    public Guid SupplierProfileId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public DateOnly? ExpiresOn { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public string ReviewStatus { get; set; } = "Pending";

    public string? RejectionReason { get; set; }
}
