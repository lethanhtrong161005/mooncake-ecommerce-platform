namespace Mooncake.EcommercePlatform.Application.DTOs.Catalog.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Atomic signed adjustment to available inventory.</summary>
public sealed record AdjustStockRequest
{
    [Range(-1_000_000, 1_000_000)]
    public int QuantityDelta { get; init; }

    [Required, MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
