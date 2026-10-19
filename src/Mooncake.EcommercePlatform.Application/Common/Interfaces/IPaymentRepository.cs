namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

public interface IPaymentRepository
{
    Task<Order?> GetOrderForPaymentAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<(Contract? Contract, ContractMilestone? Milestone)> GetMilestoneForPaymentAsync(Guid customerId, Guid contractId, Guid milestoneId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByIdempotentReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentByIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<bool> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> ConfirmPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Payment?> RefundPaymentAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<VnPaySettlementResult> ProcessVnPayResultAsync(VnPayCallback callback, CancellationToken cancellationToken = default);
    Task<int> ExpirePendingPaymentsAsync(CancellationToken cancellationToken = default);
}

public sealed record VnPaySettlementResult(string RspCode, string Message);
