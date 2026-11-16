namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Xunit;

public sealed class VnPayGatewayTests
{
    [Fact]
    public void CreatePaymentUrl_GeneratesAUrlWhoseSignatureCanBeVerified()
    {
        var gateway = new VnPayGateway(Options.Create(new VnPayOptions
        {
            TmnCode = "TESTCODE",
            HashSecret = "test-secret",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        }));
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = "PAY-20261029-TEST001",
            Amount = 125000m,
            Currency = "VND",
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        var url = gateway.CreatePaymentUrl(payment, "http://localhost:5173/#/payment-result", "127.0.0.1",
            new DateTime(2026, 10, 29, 8, 30, 0, DateTimeKind.Utc));
        var parameters = ParseQuery(new Uri(url).Query);

        Assert.Equal("12500000", parameters["vnp_Amount"]);
        Assert.Equal("20261029153000", parameters["vnp_CreateDate"]);
        Assert.Equal("127.0.0.1", parameters["vnp_IpAddr"]);
        Assert.Equal(payment.PaymentNumber, parameters["vnp_TxnRef"]);
        Assert.True(gateway.IsValidSignature(parameters));

        parameters["vnp_Amount"] = "1";
        Assert.False(gateway.IsValidSignature(parameters));
    }

    [Theory]
    [InlineData("::ffff:203.0.113.10, 172.18.0.1", "203.0.113.10")]
    [InlineData("::1", "127.0.0.1")]
    [InlineData("", "127.0.0.1")]
    public void CreatePaymentUrl_NormalizesClientIpToIpv4(string rawIp, string expectedIp)
    {
        var gateway = new VnPayGateway(Options.Create(new VnPayOptions
        {
            TmnCode = "TESTCODE",
            HashSecret = "test-secret",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        }));
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentNumber = "PAY-20261029-TEST002",
            Amount = 500000m,
            Currency = "VND",
            Method = PaymentMethod.VNPay,
            Status = PaymentStatus.Pending
        };

        var url = gateway.CreatePaymentUrl(payment, "https://staging.example.com/thanh-toan/ket-qua", rawIp,
            new DateTime(2026, 10, 29, 8, 30, 0, DateTimeKind.Utc));
        var parameters = ParseQuery(new Uri(url).Query);

        Assert.Equal(expectedIp, parameters["vnp_IpAddr"]);
        Assert.True(gateway.IsValidSignature(parameters));
    }

    private static Dictionary<string, string> ParseQuery(string query) => QueryHelpers.ParseQuery(query)
        .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
}
