namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>Explicit decision against the exact submission and version inspected by the administrator.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReviewSupplierRequest
{
    [Required] public Guid SubmissionId { get; init; }
    [Required, Range(0, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required] public bool? Approve { get; init; }
    [MaxLength(1000)] public string? RejectionReason { get; init; }
}
