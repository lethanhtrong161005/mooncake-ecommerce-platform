namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps packaging entities to API response records.</summary>
public static class CustomPackagingResponseHelper
{
    /// <summary>Builds a packaging response with a chronological revision timeline.</summary>
    public static CustomPackagingResponse ToResponse(CustomPackaging packaging,
        IReadOnlyList<CustomPackagingRevision> revisions) =>
        new(packaging.Id, packaging.OrderId, packaging.CompanyName ?? string.Empty,
            packaging.CompanyLogoUrl ?? string.Empty, packaging.BoxDesignUrl,
            packaging.PackagingType ?? string.Empty, packaging.ColorScheme,
            packaging.SpecialMessage, packaging.Notes, packaging.Status, packaging.CreatedAt,
            revisions.OrderBy(revision => revision.RevisionNumber).Select(revision =>
                new CustomPackagingRevisionResponse(revision.Id, revision.RevisionNumber,
                    revision.MockupUrl ?? string.Empty, revision.SupplierNotes,
                    revision.CustomerNotes, revision.DesignFee, revision.Status,
                    revision.SubmittedAt, revision.ReviewedAt)).ToList());
}
