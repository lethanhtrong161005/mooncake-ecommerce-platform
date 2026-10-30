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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class ContractsControllerHttpTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly Guid MilestoneId = Guid.NewGuid();
    private static readonly Guid DisputeId = Guid.NewGuid();

    private readonly Mock<IContractService> contractService = new();

    private HttpClient CreateClient(string role = "Customer")
    {
        var userId = role switch
        {
            "Supplier" => SupplierId,
            "Admin" => AdminId,
            _ => CustomerId
        };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ContractsController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(contractService.Object);
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
                new Claim(ClaimTypes.Role, role),
                new Claim("supplier_fulfillment", role == "Supplier" ? "true" : "false")
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

    // =========================================================================
    // FEATURE #15: HỢP ĐỒNG ĐIỆN TỬ & KÝ SỐ OTP (E-CONTRACT & OTP SIGNATURE)
    // =========================================================================

    [Fact]
    public async Task GetMine_Customer_Returns200WithContractList()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.Active,
            DateTime.UtcNow, DateTime.UtcNow, "sha256hash");

        contractService.Setup(s => s.GetMineAsync(CustomerId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contract]);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/contracts/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("HD-2026-TEST01", json);
    }

    [Fact]
    public async Task GetMine_Supplier_Returns200WithContractList()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.Active,
            DateTime.UtcNow, DateTime.UtcNow, "sha256hash");

        contractService.Setup(s => s.GetMineAsync(SupplierId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contract]);

        using var client = CreateClient("Supplier");
        var response = await client.GetAsync("/api/v1/contracts/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("HD-2026-TEST01", json);
    }

    [Fact]
    public async Task GetAsync_Returns200WithContractDetails()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.PendingSignature,
            null, null, null);

        contractService.Setup(s => s.GetAsync(CustomerId, ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/contracts/{ContractId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("PendingSignature", json);
    }

    [Fact]
    public async Task RequestSignatureOtp_Returns200_WhenRequested()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.PendingSignature,
            null, null, null);

        contractService.Setup(s => s.RequestSignatureOtpAsync(CustomerId, ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        using var client = CreateClient("Customer");
        var response = await client.PostAsync($"/api/v1/contracts/{ContractId}/signature-otp", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BeginSignature_Returns200_WhenBegun()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.PendingSignature,
            null, null, null);

        contractService.Setup(s => s.BeginSignatureAsync(CustomerId, ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        using var client = CreateClient("Customer");
        var response = await client.PostAsync($"/api/v1/contracts/{ContractId}/submit-for-signature", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignAsync_Returns200_WhenValidOtpProvided()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Hợp đồng Cung cấp Quà tặng", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.PendingDeposit,
            DateTime.UtcNow, null, "sha256hash");

        contractService.Setup(s => s.SignAsync(CustomerId, ContractId, "123456", true,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        using var client = CreateClient("Customer");
        var request = new SignContractRequest { OtpCode = "123456", ConfirmTerms = true };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/sign", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("PendingDeposit", json);
    }

    [Fact]
    public async Task SignAsync_Returns400_WhenOtpIsInvalid()
    {
        using var client = CreateClient("Customer");
        var invalid = new SignContractRequest { OtpCode = "12", ConfirmTerms = true }; // RegularExpression 6 digits
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/sign", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // FEATURE #16: THEO DÕI TIẾN ĐỘ & MỐC HỢP ĐỒNG (MILESTONES & ESCROW)
    // =========================================================================

    [Fact]
    public async Task GetMilestones_Returns200WithMilestoneList()
    {
        var milestones = new List<ContractMilestoneResponse>
        {
            new(MilestoneId, 1, "Đặt cọc M1", "Deposit 30%", true, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                DateOnly.FromDateTime(DateTime.UtcNow), 30000000m, 30m, MilestoneStatus.CompletedOnTime, null, null)
        };

        contractService.Setup(s => s.GetMilestonesAsync(CustomerId, ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);

        using var client = CreateClient("Customer");
        var response = await client.GetAsync($"/api/v1/contracts/{ContractId}/milestones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Đặt cọc M1", json);
    }

    [Fact]
    public async Task SubmitMilestoneProgress_Returns200_WhenSupplierSubmits()
    {
        var milestone = new ContractMilestoneResponse(
            MilestoneId, 2, "Production M2", "Desc", false, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            null, 50000000m, 50m, MilestoneStatus.PendingReview, "Done 500 boxes", ["https://example.com/photo.jpg"]);

        contractService.Setup(s => s.SubmitMilestoneProgressAsync(SupplierId, ContractId, MilestoneId,
                It.IsAny<SubmitMilestoneProgressRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestone);

        using var client = CreateClient("Supplier");
        var request = new SubmitMilestoneProgressRequest
        {
            ProgressNotes = "Done 500 boxes",
            EvidenceUrls = ["https://example.com/photo.jpg"]
        };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/milestones/{MilestoneId}/submit-progress", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Production M2", json);
    }

    [Fact]
    public async Task VerifyMilestone_Returns200_WhenCustomerApproves()
    {
        var milestone = new ContractMilestoneResponse(
            MilestoneId, 2, "Production M2", "Desc", false, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateOnly.FromDateTime(DateTime.UtcNow), 50000000m, 50m, MilestoneStatus.CompletedOnTime,
            "Done 500 boxes", ["https://example.com/photo.jpg"]);

        contractService.Setup(s => s.VerifyMilestoneAsync(CustomerId, ContractId, MilestoneId,
                It.IsAny<VerifyMilestoneRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestone);

        using var client = CreateClient("Customer");
        var request = new VerifyMilestoneRequest { IsApproved = true };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/milestones/{MilestoneId}/verify", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("CompletedOnTime", json);
    }

    // =========================================================================
    // FEATURE #17: XỬ LÝ VI PHẠM & TRANH CHẤP (EXTENSIONS & DISPUTES)
    // =========================================================================

    [Fact]
    public async Task RequestExtension_Returns200_WhenSupplierRequests()
    {
        var ext = new ContractExtensionResponse(
            Guid.NewGuid(), ContractId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            "Delays in raw ingredients", "Pending", DateTime.UtcNow, null);

        contractService.Setup(s => s.RequestExtensionAsync(SupplierId, ContractId,
                It.IsAny<RequestContractExtensionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ext);

        using var client = CreateClient("Supplier");
        var request = new RequestContractExtensionRequest
        {
            ProposedDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            Reason = "Delays in raw ingredients"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/extension-request", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Delays in raw ingredients", json);
    }

    [Fact]
    public async Task RespondExtension_Returns200_WhenCustomerApproves()
    {
        var ext = new ContractExtensionResponse(
            Guid.NewGuid(), ContractId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            "Delays in raw ingredients", "Approved", DateTime.UtcNow, "Chấp thuận gia hạn");

        contractService.Setup(s => s.RespondExtensionAsync(CustomerId, ContractId,
                It.IsAny<RespondContractExtensionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ext);

        using var client = CreateClient("Customer");
        var request = new RespondContractExtensionRequest
        {
            IsApproved = true,
            ResponseNote = "Chấp thuận gia hạn"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/extension-request/respond", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Approved", json);
    }

    [Fact]
    public async Task RaiseDispute_Returns200_WhenCustomerRaises()
    {
        var dispute = new ContractDisputeResponse(
            DisputeId, ContractId, CustomerId, "Chất lượng bánh không đạt chuẩn yêu cầu",
            ["https://example.com/evidence1.jpg"], "Active", null, null, 0m, DateTime.UtcNow, null);

        contractService.Setup(s => s.RaiseDisputeAsync(CustomerId, ContractId,
                It.IsAny<RaiseContractDisputeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

        using var client = CreateClient("Customer");
        var request = new RaiseContractDisputeRequest
        {
            DisputeReason = "Chất lượng bánh không đạt chuẩn yêu cầu",
            EvidenceUrls = ["https://example.com/evidence1.jpg"]
        };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/dispute", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Chất lượng bánh", json);
    }

    // =========================================================================
    // FEATURE #18: NGHIỆM THU TỔNG THỂ & QUYẾT TOÁN HỢP ĐỒNG (FINAL ACCEPTANCE)
    // =========================================================================

    [Fact]
    public async Task RequestFinalAcceptance_Returns200_WhenSupplierRequests()
    {
        contractService.Setup(s => s.RequestFinalAcceptanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateClient("Supplier");
        var response = await client.PostAsync($"/api/v1/contracts/{ContractId}/request-final-acceptance", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmFinalAcceptance_Returns200_WhenCustomerConfirms()
    {
        var contract = new ContractResponse(
            ContractId, "HD-2026-TEST01", null, "Contract Title", 100000000m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, ContractStatus.CompletedOnTime,
            DateTime.UtcNow, DateTime.UtcNow, "sha256hash", 30000000m, DateTime.UtcNow, DateTime.UtcNow);

        contractService.Setup(s => s.ConfirmFinalAcceptanceAsync(CustomerId, ContractId,
                It.IsAny<ConfirmFinalAcceptanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contract);

        using var client = CreateClient("Customer");
        var request = new ConfirmFinalAcceptanceRequest
        {
            ConfirmedQualityAndQuantity = true,
            AcceptanceNotes = "Bánh giao đúng hẹn và chất lượng tuyệt vời"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/contracts/{ContractId}/confirm-final-acceptance", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("CompletedOnTime", json);
    }

    // =========================================================================
    // ADMIN DISPUTE RESOLUTION ENDPOINTS (ADMIN-ONLY)
    // =========================================================================

    [Fact]
    public async Task AdminResolveDispute_Returns200_WhenAdminResolves()
    {
        var dispute = new ContractDisputeResponse(
            DisputeId, ContractId, CustomerId, "Defective packaging",
            ["https://example.com/defect.jpg"], "Resolved", "Full refund granted",
            "FullRefund", 30000000m, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        contractService.Setup(s => s.ResolveDisputeAsync(AdminId, ContractId,
                It.IsAny<AdminResolveDisputeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

        using var client = CreateClient("Admin");
        var request = new AdminResolveDisputeRequest
        {
            ResolutionType = "FullRefund",
            RefundAmount = 30000000m,
            AdminVerdict = "Full refund granted"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/admin/contracts/{ContractId}/resolve-dispute", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("FullRefund", json);
    }

    [Fact]
    public async Task AdminListDisputes_Returns200_WhenAdminQueries()
    {
        var disputeItem = new ContractDisputeListItemResponse(
            DisputeId, ContractId, "HD-2026-TEST01", "Doanh nghiệp A", "Xưởng bánh B",
            "Tranh chấp trễ hạn", null, "Active", null, null, 0m, 30000000m, DateTime.UtcNow, null);

        contractService.Setup(s => s.ListDisputesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([disputeItem]);

        using var client = CreateClient("Admin");
        var response = await client.GetAsync("/api/v1/admin/disputes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Doanh nghiệp A", json);
    }

    [Fact]
    public async Task AdminResolveDisputeById_Returns200_WhenAdminResolves()
    {
        var dispute = new ContractDisputeResponse(
            DisputeId, ContractId, CustomerId, "Defective packaging",
            null, "Resolved", "Refund approved",
            "FullRefund", 20000000m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        contractService.Setup(s => s.ResolveDisputeByIdAsync(AdminId, DisputeId,
                It.IsAny<AdminResolveDisputeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);

        using var client = CreateClient("Admin");
        var request = new AdminResolveDisputeRequest
        {
            ResolutionType = "FullRefund",
            RefundAmount = 20000000m,
            AdminVerdict = "Refund approved"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/admin/disputes/{DisputeId}/resolve", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Refund approved", json);
    }

    [Fact]
    public async Task AdminDisputeEndpoints_CustomerRole_Returns403Forbidden()
    {
        using var client = CreateClient("Customer");
        var response = await client.GetAsync("/api/v1/admin/disputes");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public static class TestServerExtensions
{
    public static HttpClient GetTestClient(this WebApplication app)
    {
        var server = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var address = addresses?.Addresses.FirstOrDefault() ?? "http://127.0.0.1:5000";
        return new HttpClient { BaseAddress = new Uri(address) };
    }
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var ticket = new AuthenticationTicket(Context.User, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        var identity = new ClaimsIdentity("Test");
        var principal = new ClaimsPrincipal(identity);
        var defaultTicket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(defaultTicket));
    }
}
