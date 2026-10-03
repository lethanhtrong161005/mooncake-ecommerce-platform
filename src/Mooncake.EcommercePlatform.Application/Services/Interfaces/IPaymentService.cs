namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

/// <summary>Payment processing, anti-collision idempotency, milestone/order settlement, and refunds.</summary>
public interface IPaymentService
{
    Task<PaymentResponse> ProcessPaymentAsync(PayRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundPaymentAsync(RefundRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentResponse>> GetPaymentsAsync(long? orderId = null, long? milestoneId = null, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetPaymentByIdAsync(long id, CancellationToken cancellationToken = default);
}
