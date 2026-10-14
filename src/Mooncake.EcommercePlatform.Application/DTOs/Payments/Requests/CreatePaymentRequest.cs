namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;

using System.ComponentModel.DataAnnotations;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record CreatePaymentRequest
{
    public Guid? OrderId { get; init; }
    public Guid? ContractId { get; init; }
    public Guid? MilestoneId { get; init; }
    [EnumDataType(typeof(PaymentMethod))] public PaymentMethod Method { get; init; } = PaymentMethod.BankTransfer;
}

public sealed record ConfirmPaymentRequest
{
    [Required, MinLength(3), MaxLength(1000)] public string Reason { get; init; } = string.Empty;
}

public sealed record RefundPaymentRequest
{
    [Required, MinLength(3), MaxLength(1000)] public string Reason { get; init; } = string.Empty;
}
