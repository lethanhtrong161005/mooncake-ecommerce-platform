namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Data access contract for payments and transactions.</summary>
public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByTransactionRefAsync(string transactionRef, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsAsync(long? orderId = null, long? milestoneId = null, CancellationToken cancellationToken = default);
}
