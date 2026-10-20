namespace Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;

using System.ComponentModel.DataAnnotations;

public sealed record RevokeSupplierRequest
{
    [Required, MinLength(10), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}
