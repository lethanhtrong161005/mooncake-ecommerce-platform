namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mooncake.EcommercePlatform.Application.DTOs.Authen.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Suppliers.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module1SuppliersControllerHttpTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();
    private static readonly Guid DocumentId = Guid.NewGuid();
    private static readonly Guid SubmissionId = Guid.NewGuid();

    private readonly Mock<ISupplierService> supplierService = new();
    private readonly Mock<IVerificationNotificationService> verificationNotificationService = new();
    private readonly Mock<INotificationService> notificationService = new();

    private HttpClient CreateClient(string role = "Supplier", bool authenticated = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SuppliersController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        builder.Services.AddSingleton(supplierService.Object);
        builder.Services.AddSingleton(verificationNotificationService.Object);
        builder.Services.AddSingleton(notificationService.Object);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();

        if (authenticated)
        {
            app.Use(async (context, next) =>
            {
                var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                ], "Test");

                context.User = new ClaimsPrincipal(identity);
                await next();
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();

        return app.GetTestClient();
    }

    // =========================================================================
    // FEATURE #2: SUPPLIER ONBOARDING & PROFILE (OWNER ACTIONS)
    // =========================================================================

    [Fact]
    public async Task StartOnboarding_Returns200OK_WithUserResponse()
    {
        var client = CreateClient("Customer");

        var responseData = new AuthenUserResponse(UserId, "0987654321", "Supplier Candidate", UserRole.Customer, UserRole.Supplier);
        supplierService.Setup(s => s.StartOnboardingAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        var response = await client.PostAsync("/api/v1/suppliers/me/onboarding", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Supplier Candidate", json.GetProperty("data").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task GetMyProfile_Returns200OK_WithSupplierProfile()
    {
        var client = CreateClient("Supplier");

        var profile = new SupplierProfileResponse(
            ProfileId, 1, "Draft", null,
            "Bánh Trung Thu Hoàng Kim", "0312345678", "Hà Nội", "Xưởng bánh truyền thống",
            null, [], ["UploadDocument", "Submit"], null, null, null);

        supplierService.Setup(s => s.GetMyProfileAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var response = await client.GetAsync("/api/v1/suppliers/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bánh Trung Thu Hoàng Kim", json.GetProperty("data").GetProperty("companyName").GetString());
        Assert.Equal("0312345678", json.GetProperty("data").GetProperty("taxCode").GetString());
    }

    [Fact]
    public async Task Apply_Returns200OK_OnSubmission()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new ApplySupplierRequest
        {
            ExpectedVersion = 1,
            CompanyName = "Bánh Trung Thu Hoàng Kim",
            TaxCode = "0312345678",
            Address = "123 Hoàn Kiếm, Hà Nội",
            Description = "Xưởng bánh",
            DocumentIds = [DocumentId]
        };

        var result = new VerificationCommandResponse(ProfileId, SubmissionId, 1, 2, SupplierVerificationStatus.Pending, "Queued");

        supplierService.Setup(s => s.ApplyAsync(UserId, It.IsAny<string>(), It.IsAny<ApplySupplierRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await client.PostAsJsonAsync("/api/v1/suppliers/me/apply", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ProfileId.ToString(), json.GetProperty("data").GetProperty("profileId").GetString());
        Assert.Equal("Pending", json.GetProperty("data").GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task UploadDocument_Returns201Created_OnMultipartFile()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-pdf-content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "gpkd.pdf");
        content.Add(new StringContent("BusinessLicense"), "documentType");

        var docResponse = new SupplierDocumentResponse(
            DocumentId, "BusinessLicense", "gpkd.pdf", "application/pdf", 16, "Ready", null, "Pending", null);

        supplierService.Setup(s => s.UploadAsync(UserId, It.IsAny<string>(), It.IsAny<Stream>(), "gpkd.pdf", "BusinessLicense", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docResponse);

        var response = await client.PostAsync("/api/v1/suppliers/me/documents", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(DocumentId.ToString(), json.GetProperty("data").GetProperty("documentId").GetString());
    }

    [Fact]
    public async Task GetDocument_Returns200OK()
    {
        var client = CreateClient("Supplier");

        var docResponse = new SupplierDocumentResponse(
            DocumentId, "FoodSafetyCertificate", "attp.pdf", "application/pdf", 1024, "Ready", null, "Approved", null);

        supplierService.Setup(s => s.GetDocumentAsync(UserId, DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docResponse);

        var response = await client.GetAsync($"/api/v1/suppliers/me/documents/{DocumentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("attp.pdf", json.GetProperty("data").GetProperty("originalFileName").GetString());
    }

    [Fact]
    public async Task DeleteDocument_Returns200OK()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        supplierService.Setup(s => s.DeleteDocumentAsync(UserId, DocumentId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.DeleteAsync($"/api/v1/suppliers/me/documents/{DocumentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DocumentContent_ReturnsFile_WithNoSniffHeader()
    {
        var client = CreateClient("Supplier");

        var fileBytes = Encoding.UTF8.GetBytes("sample-document-content");
        supplierService.Setup(s => s.ReadDocumentAsync(UserId, DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileBytes, "application/pdf", "safe_doc.pdf"));

        var response = await client.GetAsync($"/api/v1/suppliers/documents/{DocumentId}/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, bytes);
    }

    [Fact]
    public async Task MyHistory_Returns200OK_WithHistoryPage()
    {
        var client = CreateClient("Supplier");

        var historyList = new List<SupplierHistoryResponse>
        {
            new(Guid.NewGuid(), "Submitted", "Draft", "Pending", UserId, null, DateTime.UtcNow, null)
        };
        var historyPage = new VerificationPage<SupplierHistoryResponse>(historyList, 1, 20, 1);

        supplierService.Setup(s => s.HistoryAsync(UserId, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historyPage);

        var response = await client.GetAsync("/api/v1/suppliers/me/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task MySubmission_Returns200OK_WithSubmission()
    {
        var client = CreateClient("Supplier");

        var subResponse = new SupplierSubmissionResponse(
            SubmissionId, 1, "Bánh Trung Thu Hoàng Kim", "0312345678", "Hà Nội", null,
            SupplierVerificationStatus.Pending, DateTime.UtcNow, null, null, []);

        supplierService.Setup(s => s.GetSubmissionAsync(UserId, null, SubmissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subResponse);

        var response = await client.GetAsync($"/api/v1/suppliers/me/submissions/{SubmissionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(SubmissionId.ToString(), json.GetProperty("data").GetProperty("id").GetString());
    }

    // =========================================================================
    // FEATURE #2: ADMIN VERIFICATION & AUDIT ENDPOINTS
    // =========================================================================

    [Fact]
    public async Task Applications_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        var appList = new List<SupplierApplicationResponse>
        {
            new(ProfileId, SubmissionId, 1, 2, "Bánh Trung Thu Hoàng Kim", "0312345678", SupplierVerificationStatus.Pending, DateTime.UtcNow, null, 2)
        };
        var appPage = new VerificationPage<SupplierApplicationResponse>(appList, 1, 20, 1);

        supplierService.Setup(s => s.ListAsync(UserId, It.IsAny<SupplierApplicationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appPage);

        var response = await client.GetAsync("/api/v1/suppliers/applications?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Applications_Returns403Forbidden_ForCustomer()
    {
        var client = CreateClient("Customer");

        var response = await client.GetAsync("/api/v1/suppliers/applications");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pending_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        var appList = new List<SupplierApplicationResponse>
        {
            new(ProfileId, SubmissionId, 1, 2, "Bánh Trung Thu Hoàng Kim", "0312345678", SupplierVerificationStatus.Pending, DateTime.UtcNow, null, 2)
        };
        var appPage = new VerificationPage<SupplierApplicationResponse>(appList, 1, 20, 1);

        supplierService.Setup(s => s.ListAsync(UserId, It.IsAny<SupplierApplicationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appPage);

        var response = await client.GetAsync("/api/v1/suppliers/pending?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerificationDetail_Returns200OK_ForAdmin()
    {
        var client = CreateClient("Admin");

        var profile = new SupplierProfileResponse(
            ProfileId, 2, "Pending", SupplierVerificationStatus.Pending,
            "Bánh Trung Thu Hoàng Kim", "0312345678", "Hà Nội", "Xưởng bánh",
            null, [], ["Review"], null, null, null);

        supplierService.Setup(s => s.GetProfileAsync(UserId, ProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var response = await client.GetAsync($"/api/v1/suppliers/{ProfileId}/verification");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bánh Trung Thu Hoàng Kim", json.GetProperty("data").GetProperty("companyName").GetString());
    }

    [Fact]
    public async Task Review_Returns200OK_WhenAdminApproves()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new ReviewSupplierRequest
        {
            SubmissionId = SubmissionId,
            ExpectedVersion = 2,
            Approve = true,
            RejectionReason = null
        };
        var result = new VerificationCommandResponse(ProfileId, SubmissionId, 1, 3, SupplierVerificationStatus.Verified, "Queued");

        supplierService.Setup(s => s.ReviewAsync(ProfileId, UserId, It.IsAny<string>(), It.IsAny<ReviewSupplierRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await client.PostAsJsonAsync($"/api/v1/suppliers/{ProfileId}/review", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Verified", json.GetProperty("data").GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task Review_Returns403Forbidden_ForCustomer()
    {
        var client = CreateClient("Customer");
        var request = new ReviewSupplierRequest
        {
            SubmissionId = SubmissionId,
            ExpectedVersion = 2,
            Approve = true,
            RejectionReason = null
        };

        var response = await client.PostAsJsonAsync($"/api/v1/suppliers/{ProfileId}/review", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_Returns200OK_WhenAdminRevokes()
    {
        var client = CreateClient("Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new RevokeSupplierRequest(3, "Phát hiện giấy phép hết hạn và không cập nhật.");
        var result = new VerificationCommandResponse(ProfileId, null, 1, 4, SupplierVerificationStatus.Revoked, "Queued");

        supplierService.Setup(s => s.RevokeAsync(ProfileId, UserId, It.IsAny<string>(), It.IsAny<RevokeSupplierRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await client.PostAsJsonAsync($"/api/v1/suppliers/{ProfileId}/revoke", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Revoked", json.GetProperty("data").GetProperty("verificationStatus").GetString());
    }

    // =========================================================================
    // FEATURE #2: VERIFICATION NOTIFICATIONS ENDPOINTS
    // =========================================================================

    [Fact]
    public async Task Notifications_ListVerification_Returns200OK()
    {
        var client = CreateClient("Supplier");

        var notice = new VerificationNotificationResponse(
            Guid.NewGuid(), "Approved", "Hồ sơ đã được phê duyệt", "Chúc mừng! Doanh nghiệp đã được cấp huy hiệu Đã xác minh.",
            ProfileId, null, 3, 1, false, DateTime.UtcNow);

        var inbox = new VerificationInboxResponse([notice], 1, 20, 1, 1, "token-boundary-123");

        verificationNotificationService.Setup(s => s.InboxAsync(UserId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inbox);

        var response = await client.GetAsync("/api/v1/notifications?category=SupplierVerification&page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("data").GetProperty("totalCount").GetInt32());
        Assert.Equal("token-boundary-123", json.GetProperty("data").GetProperty("readThroughToken").GetString());
    }

    [Fact]
    public async Task Notifications_UnreadCount_Returns200OK()
    {
        var client = CreateClient("Supplier");

        var inbox = new VerificationInboxResponse([], 1, 1, 5, 3, "token-boundary-456");
        verificationNotificationService.Setup(s => s.InboxAsync(UserId, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inbox);

        var response = await client.GetAsync("/api/v1/notifications/unread-count?category=SupplierVerification");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, json.GetProperty("data").GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Notifications_MarkAsRead_Returns200OK()
    {
        var client = CreateClient("Supplier");
        var noticeId = Guid.NewGuid();

        verificationNotificationService.Setup(s => s.ReadAsync(UserId, noticeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await client.PatchAsync($"/api/v1/notifications/{noticeId}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_ReadAll_Returns200OK()
    {
        var client = CreateClient("Supplier");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        verificationNotificationService.Setup(s => s.ReadAllAsync(UserId, "token-xyz", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var body = new ReadVerificationNotificationsRequest("token-xyz", "SupplierVerification");
        var response = await client.PostAsJsonAsync("/api/v1/notifications/read-all", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
