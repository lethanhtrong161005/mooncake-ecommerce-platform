namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Owner/admin-only verification state; never exposes private provider URLs.</summary>
public sealed record SupplierProfileResponse(Guid? ProfileId, long Version, string OnboardingState,
    SupplierVerificationStatus? VerificationStatus, string? CompanyName, string? TaxCode,
    string? Address, string? Description, SupplierSubmissionResponse? CurrentSubmission,
    IReadOnlyList<SupplierDocumentResponse> StagedDocuments, IReadOnlyList<string> AllowedActions,
    DateTime? VerifiedAtUtc, DateTime? RevokedAtUtc, string? RevocationReason);

/// <summary>Server-confirmed document metadata with object access enforced by the content API.</summary>
public sealed record SupplierDocumentResponse(Guid DocumentId, string DocumentType, string? OriginalFileName,
    string? ContentType, long? FileSizeBytes, string UploadStatus, DateOnly? ExpiresOn,
    string ReviewStatus, string? RejectionReason);
