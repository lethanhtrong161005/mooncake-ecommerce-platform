namespace Mooncake.EcommercePlatform.Application.DTOs.Payments.Models;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Enums;

public sealed record OrderPaymentTarget(OrderStatus Status, decimal TotalAmount);
public sealed record MilestonePaymentTarget(ContractStatus ContractStatus, MilestoneStatus MilestoneStatus, decimal Amount);
public sealed record PaymentData(Guid? OrderId, Guid? ContractId, Guid? MilestoneId, PaymentMethod Method, PaymentResponse Response);
public sealed record CreatePaymentData(Guid? OrderId, Guid? ContractId, Guid? MilestoneId, Guid CustomerId,
    string PaymentNumber, string IdempotencyKey, decimal Amount, PaymentMethod Method, DateTime ExpiresAtUtc);
