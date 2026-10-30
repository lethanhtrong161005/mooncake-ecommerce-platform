namespace Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;

using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Result of a customer packaging review decision.</summary>
public sealed record PackagingReviewResponse(
    CustomPackagingStatus PackagingStatus,
    string? ApprovedMockupUrl,
    DateTime ReviewedAtUtc);
