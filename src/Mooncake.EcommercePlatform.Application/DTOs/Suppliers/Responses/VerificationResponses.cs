namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Bounded query results with stable page metadata.</summary>
public sealed record VerificationPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Immutable submitted business details and their reviewed document set.</summary>
public sealed record SupplierSubmissionResponse(Guid Id, int Revision, string CompanyName, string TaxCode,
    string Address, string? Description, SupplierVerificationStatus Status, DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc, string? RejectionReason, IReadOnlyList<SupplierDocumentResponse> Documents, string Source = "Web");

/// <summary>Lightweight queue row; intentionally excludes private file locations.</summary>
public sealed record SupplierApplicationResponse(Guid ProfileId, Guid? SubmissionId, int Revision, long Version,
    string? CompanyName, string? TaxCode, SupplierVerificationStatus Status, DateTime? SubmittedAtUtc,
    DateTime? ReviewedAtUtc, int DocumentCount);

/// <summary>Auditable workflow event visible only to its owner or an administrator.</summary>
public sealed record SupplierHistoryResponse(Guid Id, string EventType, string? FromStatus,
    string? ToStatus, Guid? ActorUserId, string? Reason, DateTime CreatedAtUtc, string? Metadata);

/// <summary>Committed result; queued delivery is not a claim that email or push was received.</summary>
public sealed record VerificationCommandResponse(Guid ProfileId, Guid? SubmissionId, int Revision,
    long Version, SupplierVerificationStatus VerificationStatus, string NotificationStatus,
    bool IdempotentReplay = false);
