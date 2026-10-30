namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class PaymentsControllerHttpTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly Guid SupplierUserId = Guid.NewGuid();
    private static readonly Guid CustomerUserId = Guid.NewGuid();
    private static readonly Guid PaymentId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly Guid MilestoneId = Guid.NewGuid();

    private readonly Mock<IPaymentService> paymentService = new();

    private HttpClient CreateClient(string role = "Customer")
    {
        var userId = role == "Admin" ? AdminUserId : role == "Supplier" ? SupplierUserId : CustomerUserId;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(PaymentsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(paymentService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ], "Test");
            context.User = new ClaimsPrincipal(identity);
            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.StartAsync().GetAwaiter().GetResult();
        return app.GetTestClient();
    }

    [Fact]
    public async Task Create_Customer_Returns201Created()
    {
        paymentService.Setup(s => s.CreateAsync(CustomerUserId, It.IsAny<string?>(), It.IsAny<CreatePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResponse(
                PaymentId,
                "PAY-202610-001",
                OrderId,
                null,
                null,
                25000000m,
                "VND",
                PaymentMethod.BankTransfer,
                PaymentStatus.Pending,
                null,
                DateTime.UtcNow.AddHours(24)
            ));

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest
        {
            OrderId = OrderId,
            Method = PaymentMethod.BankTransfer
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(201, json.GetProperty("status").GetInt32());
        Assert.Equal(PaymentId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Create_Supplier_Returns403Forbidden()
    {
        using var client = CreateClient("Supplier");
        var response = await client.PostAsJsonAsync("/api/v1/payments", new CreatePaymentRequest
        {
            OrderId = OrderId,
            Method = PaymentMethod.BankTransfer
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_Customer_Returns200()
    {
        paymentService.Setup(s => s.GetMineAsync(CustomerUserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PaymentResponse(
                    PaymentId,
                    "PAY-202610-001",
                    OrderId,
                    null,
                    null,
                    25000000m,
                    "VND",
                    PaymentMethod.VNPay,
                    PaymentStatus.Completed,
                    DateTime.UtcNow,
                    null
                )
            ]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/payments/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task CreateOrderVnPayUrl_Customer_Returns200()
    {
        paymentService.Setup(s => s.CreateOrderVnPayUrlAsync(CustomerUserId, It.IsAny<CreateOrderVnPayUrlRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VnPayUrlResponse(
                PaymentId,
                "PAY-ORD-001",
                50000000m,
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=5000000000"
            ));

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/payments/orders/vnpay-url", new CreateOrderVnPayUrlRequest
        {
            OrderId = OrderId,
            ReturnUrl = "http://localhost:5186/#/thanh-toan/ket-qua"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Contains("sandbox.vnpayment.vn", json.GetProperty("data").GetProperty("paymentUrl").GetString());
    }

    [Fact]
    public async Task CreateOrderVnPayUrl_Supplier_Returns403Forbidden()
    {
        using var client = CreateClient("Supplier");
        var response = await client.PostAsJsonAsync("/api/v1/payments/orders/vnpay-url", new CreateOrderVnPayUrlRequest
        {
            OrderId = OrderId,
            ReturnUrl = "http://localhost:5186/#/thanh-toan/ket-qua"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateContractVnPayUrl_Customer_Returns200()
    {
        paymentService.Setup(s => s.CreateVnPayUrlAsync(CustomerUserId, It.IsAny<CreateVnPayUrlRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VnPayUrlResponse(
                PaymentId,
                "PAY-CTR-001",
                45000000m,
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=4500000000"
            ));

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/payments/contracts/vnpay-url", new CreateVnPayUrlRequest
        {
            ContractId = ContractId,
            MilestoneId = MilestoneId,
            ReturnUrl = "http://localhost:5186/#/thanh-toan/ket-qua"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Contains("sandbox.vnpayment.vn", json.GetProperty("data").GetProperty("paymentUrl").GetString());
    }

    [Fact]
    public async Task CreateBankTransfer_Customer_Returns200WithVietQr()
    {
        paymentService.Setup(s => s.CreateBankTransferAsync(CustomerUserId, It.IsAny<CreateBankTransferRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VietQrPaymentResponse(
                PaymentId,
                "PAY-QR-001",
                15000000m,
                "Ngân hàng Quân Đội (MB Bank)",
                "970422",
                "0988668899",
                "CONG TY CP NGUYET PHO B2B",
                "PAY-QR-001 NGUYET PHO",
                "https://img.vietqr.io/image/MB-0988668899-compact.png?amount=15000000&addInfo=PAY-QR-001",
                DateTime.UtcNow.AddHours(24)
            ));

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync("/api/v1/payments/bank-transfer", new CreateBankTransferRequest
        {
            OrderId = OrderId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("Ngân hàng Quân Đội (MB Bank)", json.GetProperty("data").GetProperty("bankName").GetString());
        Assert.Equal("0988668899", json.GetProperty("data").GetProperty("accountNumber").GetString());
    }

    [Fact]
    public async Task UploadEvidence_Customer_Returns200()
    {
        paymentService.Setup(s => s.UploadEvidenceAsync(PaymentId, CustomerUserId, It.IsAny<UploadPaymentEvidenceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResponse(
                PaymentId,
                "PAY-QR-001",
                OrderId,
                null,
                null,
                15000000m,
                "VND",
                PaymentMethod.BankTransfer,
                PaymentStatus.Pending,
                null,
                null,
                "https://storage.mooncake.vn/evidence/unc-123.jpg"
            ));

        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync($"/api/v1/payments/{PaymentId}/evidence", new UploadPaymentEvidenceRequest
        {
            EvidenceUrl = "https://storage.mooncake.vn/evidence/unc-123.jpg",
            Notes = "Đã chuyển khoản thành công từ tài khoản VCB"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("https://storage.mooncake.vn/evidence/unc-123.jpg", json.GetProperty("data").GetProperty("evidenceUrl").GetString());
    }

    [Fact]
    public async Task VerifyReturnCallback_Anonymous_Returns200()
    {
        paymentService.Setup(s => s.VerifyReturnCallbackAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VnPayReturnCallbackResponse(
                "PAY-202610-001",
                OrderId,
                null,
                null,
                25000000m,
                true,
                "00",
                "NCB",
                "14556677",
                DateTime.UtcNow
            ));

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/payments/vnpay/return-callback?vnp_ResponseCode=00&vnp_TxnRef=PAY-202610-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.True(json.GetProperty("data").GetProperty("isSuccess").GetBoolean());
    }

    [Fact]
    public async Task ProcessVnPayIpn_Anonymous_Returns200WithRspCode()
    {
        paymentService.Setup(s => s.ProcessVnPayIpnAsync(It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("00", "Confirm Success"));

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/payments/vnpay/ipn?vnp_ResponseCode=00&vnp_TxnRef=PAY-202610-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("00", json.GetProperty("rspCode").GetString());
        Assert.Equal("Confirm Success", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetEscrowSummary_Admin_Returns200()
    {
        paymentService.Setup(s => s.GetEscrowSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EscrowSummaryResponse(
                1250000000m,
                24,
                380000000m,
                0m,
                [
                    new EscrowLedgerItemDto(
                        ContractId,
                        "HD-2026-001",
                        "Tập đoàn Vingroup",
                        "Xưởng Bánh Tràng An Nghệ Nhân",
                        150000000m,
                        45000000m,
                        1,
                        3,
                        "HeldInEscrow",
                        DateTime.UtcNow.AddDays(-2)
                    )
                ]
            ));

        using var client = CreateClient("Admin");
        var response = await client.GetAsync("/api/v1/payments/escrow/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal(1250000000m, json.GetProperty("data").GetProperty("totalEscrowBalance").GetDecimal());
    }

    [Fact]
    public async Task GetEscrowSummary_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/payments/escrow/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Admin_Returns200()
    {
        paymentService.Setup(s => s.ConfirmAsync(PaymentId, AdminUserId, "Xác nhận đối soát ngân hàng khớp lệnh", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResponse(
                PaymentId,
                "PAY-202610-001",
                OrderId,
                null,
                null,
                25000000m,
                "VND",
                PaymentMethod.BankTransfer,
                PaymentStatus.Completed,
                DateTime.UtcNow,
                null
            ));

        using var client = CreateClient("Admin");
        var response = await client.PostAsJsonAsync($"/api/v1/payments/{PaymentId}/confirm", new ConfirmPaymentRequest
        {
            Reason = "Xác nhận đối soát ngân hàng khớp lệnh"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("Completed", json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Confirm_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync($"/api/v1/payments/{PaymentId}/confirm", new ConfirmPaymentRequest
        {
            Reason = "Tự xác nhận"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refund_Admin_Returns200()
    {
        paymentService.Setup(s => s.RefundAsync(PaymentId, AdminUserId, "Hoàn tiền theo thỏa thuận hủy đơn", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResponse(
                PaymentId,
                "PAY-202610-001",
                OrderId,
                null,
                null,
                25000000m,
                "VND",
                PaymentMethod.VNPay,
                PaymentStatus.Refunded,
                DateTime.UtcNow.AddDays(-1),
                null,
                null,
                25000000m,
                "Hoàn tiền theo thỏa thuận hủy đơn"
            ));

        using var client = CreateClient("Admin");
        var response = await client.PostAsJsonAsync($"/api/v1/payments/{PaymentId}/refund", new RefundPaymentRequest
        {
            Reason = "Hoàn tiền theo thỏa thuận hủy đơn"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(200, json.GetProperty("status").GetInt32());
        Assert.Equal("Refunded", json.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Refund_Customer_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.PostAsJsonAsync($"/api/v1/payments/{PaymentId}/refund", new RefundPaymentRequest
        {
            Reason = "Tự hoàn tiền"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

