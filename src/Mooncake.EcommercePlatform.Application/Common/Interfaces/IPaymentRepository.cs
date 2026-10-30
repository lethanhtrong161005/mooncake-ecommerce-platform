namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;

public interface IPaymentRepository
{
    Task<Order?> GetOrderForPaymentAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<(Contract? Contract, ContractMilestone? Milestone)> GetMilestoneForPaymentAsync(Guid customerId, Guid contractId, Guid milestoneId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<bool> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> PrepareVnPayPaymentAsync(Guid customerId, Guid contractId, Guid milestoneId,
        CancellationToken cancellationToken = default);
    Task<Payment?> PrepareOrderVnPayPaymentAsync(Guid customerId, Guid orderId,
        CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByNumberAsync(string paymentNumber, CancellationToken cancellationToken = default);
    Task<Payment?> ConfirmVnPayPaymentAsync(string paymentNumber, CancellationToken cancellationToken = default);
    Task<bool> FailVnPayPaymentAsync(string paymentNumber, string reason, CancellationToken cancellationToken = default);
    Task<Payment?> ConfirmPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Payment?> RefundPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Payment?> AttachEvidenceAsync(Guid paymentId, Guid customerId, string evidenceUrl, string? notes, CancellationToken cancellationToken = default);
    Task<EscrowSummaryResponse> GetEscrowSummaryAsync(CancellationToken cancellationToken = default);
    Task<int> ExpirePendingPaymentsAsync(CancellationToken cancellationToken = default);
}
