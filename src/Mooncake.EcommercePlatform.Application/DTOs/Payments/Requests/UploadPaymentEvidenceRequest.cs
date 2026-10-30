namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer request to upload payment transfer evidence / slip.</summary>
public sealed record UploadPaymentEvidenceRequest
{
    [Required, MaxLength(1000)] public string EvidenceUrl { get; init; } = string.Empty;
    [MaxLength(500)] public string? Notes { get; init; }
}

