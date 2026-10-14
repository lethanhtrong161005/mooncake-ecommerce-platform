namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ConfirmAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
}
