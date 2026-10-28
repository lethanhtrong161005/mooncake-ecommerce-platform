namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>Owner-submitted immutable business snapshot referencing previously uploaded private documents.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ApplySupplierRequest
{
    [Required, Range(0, long.MaxValue)] public long? ExpectedVersion { get; init; }
    [Required, MaxLength(255)] public string CompanyName { get; init; } = string.Empty;
    [Required, MaxLength(50)] public string TaxCode { get; init; } = string.Empty;
    [Required, MaxLength(500)] public string Address { get; init; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; init; }
    [Required, MinLength(1), MaxLength(10)] public List<Guid> DocumentIds { get; init; } = [];
}
