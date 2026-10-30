namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>VNPay 2.1 HMAC-SHA512 URL builder and callback signature verifier.</summary>
public sealed class VnPayGateway(IOptions<VnPayOptions> options) : IVnPayGateway
{
    private readonly VnPayOptions settings = options.Value;

    public string CreatePaymentUrl(Payment payment, string returnUrl, string clientIpAddress, DateTime createdAtUtc)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0", ["vnp_Command"] = "pay", ["vnp_TmnCode"] = settings.TmnCode,
            ["vnp_Amount"] = decimal.ToInt64(decimal.Round(payment.Amount * 100m, 0)).ToString(),
            ["vnp_CreateDate"] = createdAtUtc.ToString("yyyyMMddHHmmss"), ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIpAddress) ? "127.0.0.1" : clientIpAddress,
            ["vnp_Locale"] = "vn", ["vnp_OrderInfo"] = $"Contract deposit {payment.PaymentNumber}",
            ["vnp_OrderType"] = "other", ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = payment.PaymentNumber
        };
        var secureHash = CreateSecureHash(parameters);
        return $"{settings.BaseUrl}?{BuildQuery(parameters)}&vnp_SecureHash={secureHash}";
    }

    public bool IsValidSignature(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("vnp_SecureHash", out var secureHash) || string.IsNullOrWhiteSpace(secureHash))
            return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(CreateSecureHash(parameters)),
            Encoding.UTF8.GetBytes(secureHash.ToLowerInvariant()));
    }

    private string CreateSecureHash(IReadOnlyDictionary<string, string> parameters)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(settings.HashSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildQuery(parameters)))).ToLowerInvariant();
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string> parameters) => string.Join("&",
        parameters.Where(item => !string.IsNullOrWhiteSpace(item.Value) &&
                !string.Equals(item.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));
}
