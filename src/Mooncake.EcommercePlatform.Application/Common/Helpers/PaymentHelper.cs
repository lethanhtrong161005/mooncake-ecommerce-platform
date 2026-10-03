namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Maps Payment domain entities to response DTOs.</summary>
public class PaymentHelper : IPaymentHelper
{
    public PaymentResponse ToResponse(Payment payment) =>
        new(
            payment.Id,
            payment.OrderId,
            payment.MilestoneId,
            payment.PaymentType,
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.TransactionRef,
            payment.PaidAtUtc,
            payment.CreatedAtUtc,
            payment.UpdatedAtUtc,
            payment.Order?.ReceiverName,
            payment.Milestone?.Name
        );
}
