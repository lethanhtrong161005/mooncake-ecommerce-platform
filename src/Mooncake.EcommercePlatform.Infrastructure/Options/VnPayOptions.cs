namespace Mooncake.EcommercePlatform.Infrastructure.Options;

/// <summary>Configuration required by VNPay sandbox integration.</summary>
public sealed class VnPayOptions
{
    public const string SectionName = "VnPay";
    public string TmnCode { get; init; } = string.Empty;
    public string HashSecret { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    public bool IsValid() => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret) &&
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
