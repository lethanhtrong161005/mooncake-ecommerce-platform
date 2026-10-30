namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>Customer request to create a signed VNPay checkout URL for a deposit milestone.</summary>
public sealed record CreateVnPayUrlRequest
{
    [Required] public Guid ContractId { get; init; }
    [Required] public Guid MilestoneId { get; init; }
    [Required, Url, MaxLength(2000)] public string ReturnUrl { get; init; } = string.Empty;
}
