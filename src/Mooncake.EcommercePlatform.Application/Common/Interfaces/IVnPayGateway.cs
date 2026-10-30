namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Builds and verifies VNPay sandbox requests without exposing gateway secrets to Application callers.</summary>
public interface IVnPayGateway
{
    string CreatePaymentUrl(Payment payment, string returnUrl, string clientIpAddress, DateTime createdAtUtc);
    bool IsValidSignature(IReadOnlyDictionary<string, string> parameters);
}
