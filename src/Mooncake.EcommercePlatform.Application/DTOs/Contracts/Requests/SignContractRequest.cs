namespace Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record SignContractRequest
{
    [Required, RegularExpression("^[0-9]{6}$")] public string OtpCode { get; init; } = string.Empty;
    public bool ConfirmTerms { get; init; }
}
