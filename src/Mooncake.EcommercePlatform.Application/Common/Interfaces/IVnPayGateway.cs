namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

public interface IVnPayGateway
{
    string CreatePaymentUrl(VnPayPaymentRequest request);
    bool TryValidateCallback(IReadOnlyDictionary<string, string> parameters, out VnPayCallback callback);
}

public sealed record VnPayPaymentRequest(string PaymentNumber, decimal Amount, string OrderInfo,
    string ClientIpAddress, DateTime ExpiresAtUtc);

public sealed record VnPayCallback(string PaymentNumber, string TransactionId, long Amount,
    string ResponseCode, string TransactionStatus);
