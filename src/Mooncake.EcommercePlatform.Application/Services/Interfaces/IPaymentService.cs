namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<VnPayUrlResponse> CreateVnPayUrlAsync(Guid customerId, CreateVnPayUrlRequest request,
        string? clientIpAddress, CancellationToken cancellationToken = default);
    Task<VnPayUrlResponse> CreateOrderVnPayUrlAsync(Guid customerId, CreateOrderVnPayUrlRequest request,
        string? clientIpAddress, CancellationToken cancellationToken = default);
    Task<VietQrPaymentResponse> CreateBankTransferAsync(Guid customerId, CreateBankTransferRequest request, CancellationToken cancellationToken = default);
    Task<VnPayReturnCallbackResponse> VerifyReturnCallbackAsync(IReadOnlyDictionary<string, string> queryParams, CancellationToken cancellationToken = default);
    Task<PaymentResponse> UploadEvidenceAsync(Guid paymentId, Guid customerId, UploadPaymentEvidenceRequest request, CancellationToken cancellationToken = default);
    Task<EscrowSummaryResponse> GetEscrowSummaryAsync(CancellationToken cancellationToken = default);
    Task<(string Code, string Message)> ProcessVnPayIpnAsync(IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentResponse>> GetMineAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ConfirmAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundAsync(Guid paymentId, Guid adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
}
