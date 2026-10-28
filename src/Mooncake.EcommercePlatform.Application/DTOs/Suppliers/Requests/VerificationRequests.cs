namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Version-protected revocation of a previously verified profile.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RevokeSupplierRequest(
    [Required, Range(0, long.MaxValue)] long? ExpectedVersion,
    [Required, MinLength(10), MaxLength(1000)] string Reason);

/// <summary>Bounded server-side application query with allowlisted sorting.</summary>
public sealed record SupplierApplicationQuery
{
    [Range(1, 100000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    public SupplierVerificationStatus? Status { get; init; }
    [MaxLength(100)] public string? Q { get; init; }
    public string Sort { get; init; } = "submittedAtAsc";
}
