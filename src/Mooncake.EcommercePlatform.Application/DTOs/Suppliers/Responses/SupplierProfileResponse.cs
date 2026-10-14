namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Supplier profile and current verification state.</summary>
public sealed record SupplierProfileResponse(
    Guid Id,
    Guid UserId,
    string? CompanyName,
    string? TaxCode,
    string? Address,
    string? Description,
    SupplierVerificationStatus VerificationStatus,
    DateTime? ReviewedAt,
    string? RejectionReason,
    IReadOnlyList<SupplierDocumentResponse> Documents);

/// <summary>Submitted supplier verification document metadata.</summary>
public sealed record SupplierDocumentResponse(Guid Id, string DocumentType, string FileUrl, string ReviewStatus, string? RejectionReason);
