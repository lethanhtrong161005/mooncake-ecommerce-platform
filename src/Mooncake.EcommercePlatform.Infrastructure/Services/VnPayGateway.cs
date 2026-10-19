namespace Mooncake.EcommercePlatform.Infrastructure.Services;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;

/// <summary>Builds and verifies VNPAY 2.1.0 Sandbox requests.</summary>
public sealed partial class VnPayGateway(IConfiguration configuration, IDateTimeProvider clock) : IVnPayGateway
{
    private const string DefaultPaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    private const string ApiVersion = "2.1.0";

    public string CreatePaymentUrl(VnPayPaymentRequest request)
    {
        var tmnCode = RequiredSetting("VNPAY_TMN_CODE");
        var hashSecret = RequiredSetting("VNPAY_HASH_SECRET");
        var returnUrl = RequiredSetting("VNPAY_RETURN_URL");
        var paymentUrl = configuration["VNPAY_PAYMENT_URL"] ?? DefaultPaymentUrl;
        var localNow = clock.UtcNow.AddHours(7);
        var amount = decimal.ToInt64(decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero));
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
            ["vnp_Command"] = "pay",
            ["vnp_CreateDate"] = localNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = request.ExpiresAtUtc.AddHours(7).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = NormalizeIp(request.ClientIpAddress),
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = NormalizeOrderInfo(request.OrderInfo),
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_TxnRef"] = request.PaymentNumber,
            ["vnp_Version"] = ApiVersion
        };

        var signingData = BuildQuery(parameters);
        var signature = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(hashSecret),
            Encoding.UTF8.GetBytes(signingData))).ToLowerInvariant();
        var query = $"{signingData}&vnp_SecureHash={Uri.EscapeDataString(signature)}";
        return $"{paymentUrl}?{query}";
    }

    public bool TryValidateCallback(IReadOnlyDictionary<string, string> parameters, out VnPayCallback callback)
    {
        callback = default!;
        var hashSecret = configuration["VNPAY_HASH_SECRET"];
        var tmnCode = configuration["VNPAY_TMN_CODE"];
        if (string.IsNullOrWhiteSpace(hashSecret) || string.IsNullOrWhiteSpace(tmnCode)
            || !parameters.TryGetValue("vnp_SecureHash", out var receivedHash)
            || !parameters.TryGetValue("vnp_TmnCode", out var callbackTmnCode)
            || !FixedTimeEquals(callbackTmnCode, tmnCode))
            return false;

        var signingParameters = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            if (key.StartsWith("vnp_", StringComparison.Ordinal)
                && key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
                signingParameters[key] = value;
        }

        var expectedHash = Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(hashSecret),
            Encoding.UTF8.GetBytes(BuildQuery(signingParameters)))).ToLowerInvariant();
        if (!FixedTimeEquals(expectedHash, receivedHash)
            || !parameters.TryGetValue("vnp_TxnRef", out var paymentNumber)
            || !parameters.TryGetValue("vnp_TransactionNo", out var transactionId)
            || !parameters.TryGetValue("vnp_Amount", out var rawAmount)
            || !parameters.TryGetValue("vnp_ResponseCode", out var responseCode)
            || !parameters.TryGetValue("vnp_TransactionStatus", out var transactionStatus)
            || !Guid.TryParseExact(paymentNumber, "N", out _)
            || !long.TryParse(rawAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var amount))
            return false;

        callback = new VnPayCallback(paymentNumber, transactionId, amount, responseCode, transactionStatus);
        return true;
    }

    private string RequiredSetting(string key) => configuration[key] is { Length: > 0 } value
        ? value : throw new InvalidOperationException($"{key} must be configured for VNPAY Sandbox payments.");

    private static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join('&', parameters.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value).Replace("%20", "+", StringComparison.Ordinal)}"));

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left.ToLowerInvariant());
        var rightBytes = Encoding.ASCII.GetBytes(right.ToLowerInvariant());
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string NormalizeIp(string? ipAddress) => string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress;

    private static string NormalizeOrderInfo(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed.Where(character =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
        var normalized = withoutMarks.Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace('đ', 'd').Replace('Đ', 'D');
        return WhitespaceRegex().Replace(UnsupportedOrderInfoRegex().Replace(normalized, string.Empty), " ").Trim();
    }

    [GeneratedRegex("[^a-zA-Z0-9 ]")]
    private static partial Regex UnsupportedOrderInfoRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
