namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Custom packaging details and immutable revision timeline.</summary>
public sealed record CustomPackagingResponse(
    Guid Id,
    Guid OrderId,
    string CompanyName,
    string CompanyLogoUrl,
    string? BoxDesignUrl,
    string PackagingType,
    string? ColorScheme,
    string? SpecialMessage,
    string? Notes,
    CustomPackagingStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<CustomPackagingRevisionResponse> Revisions);

/// <summary>One supplier mockup revision and its customer review state.</summary>
public sealed record CustomPackagingRevisionResponse(
    Guid Id,
    int RevisionNumber,
    string MockupUrl,
    string? SupplierNotes,
    string? CustomerNotes,
    decimal DesignFee,
    CustomPackagingStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);
