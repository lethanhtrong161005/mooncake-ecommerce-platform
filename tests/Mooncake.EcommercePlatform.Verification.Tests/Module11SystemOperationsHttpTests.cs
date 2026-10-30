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
using Mooncake.EcommercePlatform.Application.DTOs.Admin;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Contracts.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Notifications;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Shops.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Xunit;

public sealed class Module11SystemOperationsHttpTests
{
  private static readonly Guid AdminUserId = Guid.NewGuid();
  private static readonly Guid CustomerUserId = Guid.NewGuid();
  private static readonly Guid TargetUserId = Guid.NewGuid();
  private static readonly Guid DisputeId = Guid.NewGuid();
  private static readonly Guid ContractId = Guid.NewGuid();
  private static readonly Guid ShopId = Guid.NewGuid();
  private static readonly Guid SupplierId = Guid.NewGuid();
  private static readonly Guid DepositRuleId = Guid.NewGuid();
  private static readonly Guid NotificationId = Guid.NewGuid();

  private readonly Mock<IVerificationNotificationService> verificationNotifService = new();
  private readonly Mock<INotificationService> notificationService = new();
  private readonly Mock<IContractService> contractService = new();
  private readonly Mock<IAdminPlatformService> adminPlatformService = new();
  private readonly Mock<IShopService> shopService = new();

  private HttpClient CreateClient(string role = "Admin")
  {
    var userId = role == "Admin" ? AdminUserId : CustomerUserId;

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
    builder.WebHost.UseUrls("http://127.0.0.1:0");

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(AdminPlatformController).Assembly)
        .AddJsonOptions(options =>
        {
          options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
          options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

    builder.Services.AddSingleton(verificationNotifService.Object);
    builder.Services.AddSingleton(notificationService.Object);
    builder.Services.AddSingleton(contractService.Object);
    builder.Services.AddSingleton(adminPlatformService.Object);
    builder.Services.AddSingleton(shopService.Object);

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

  // =========================================================================
  // FEATURE #26: THÔNG BÁO REAL-TIME & NOTIFICATION CENTER
  // =========================================================================

  [Fact]
  public async Task Notifications_GetInbox_Returns200OK()
  {
    var client = CreateClient("Admin");

    var items = new List<NotificationItemDto>
    {
        new(NotificationId, "payment_success", "payment", "Cọc thành công", "Đã nhận 50.000.000 ₫", null, null, null, "high", false, null, DateTime.UtcNow)
    };
    notificationService.Setup(s => s.GetInboxAsync(AdminUserId, 1, 20, null, null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new NotificationInboxResponse(items, 1, 20, 1, 1, 1));

    var response = await client.GetAsync("/api/v1/notifications");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(1, json.GetProperty("data").GetProperty("totalItems").GetInt32());
  }

  [Fact]
  public async Task Notifications_GetInbox_FilteredByCategory_Returns200OK()
  {
    var client = CreateClient("Admin");

    notificationService.Setup(s => s.GetInboxAsync(AdminUserId, 1, 20, "payment", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new NotificationInboxResponse([], 1, 20, 0, 0, 0));

    var response = await client.GetAsync("/api/v1/notifications?category=payment");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task Notifications_GetUnreadCount_Returns200OK()
  {
    var client = CreateClient("Admin");

    notificationService.Setup(s => s.GetUnreadCountAsync(AdminUserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new UnreadCountResponse(4, DateTime.UtcNow));

    var response = await client.GetAsync("/api/v1/notifications/unread-count");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(4, json.GetProperty("data").GetProperty("unreadCount").GetInt32());
  }

  [Fact]
  public async Task Notifications_MarkAsRead_Returns200OK()
  {
    var client = CreateClient("Admin");

    notificationService.Setup(s => s.MarkAsReadAsync(AdminUserId, NotificationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var response = await client.PatchAsync($"/api/v1/notifications/{NotificationId}/read", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task Notifications_MarkAllAsRead_Returns200OK()
  {
    var client = CreateClient("Admin");

    notificationService.Setup(s => s.MarkAllAsReadAsync(AdminUserId, "all", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(5);

    var payload = new ReadGeneralOrVerificationNotificationRequest(null, "all", DateTime.UtcNow);
    var response = await client.PostAsJsonAsync("/api/v1/notifications/read-all", payload);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(5, json.GetProperty("data").GetProperty("updatedCount").GetInt32());
  }

  [Fact]
  public async Task Notifications_GetPreferences_Returns200OK()
  {
    var client = CreateClient("Admin");

    notificationService.Setup(s => s.GetPreferencesAsync(AdminUserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new NotificationPreferencesDto(EnableInApp: true, EnableWebPush: true, EnableEmailDigest: false));

    var response = await client.GetAsync("/api/v1/notifications/preferences");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(json.GetProperty("data").GetProperty("enableInApp").GetBoolean());
  }

  [Fact]
  public async Task Notifications_UpdatePreferences_Returns200OK()
  {
    var client = CreateClient("Admin");

    var updateDto = new NotificationPreferencesDto(EnableInApp: true, EnableWebPush: false, EnableEmailDigest: true);
    notificationService.Setup(s => s.UpdatePreferencesAsync(AdminUserId, updateDto, It.IsAny<CancellationToken>()))
        .ReturnsAsync(updateDto);

    var response = await client.PutAsJsonAsync("/api/v1/notifications/preferences", updateDto);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(json.GetProperty("data").GetProperty("enableEmailDigest").GetBoolean());
  }

  // =========================================================================
  // FEATURE #27: QUẢN TRỊ TRANH CHẤP SAFETRADE (DISPUTE RESOLUTION)
  // =========================================================================

  [Fact]
  public async Task AdminDisputes_List_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var disputes = new List<ContractDisputeListItemResponse>
    {
        new(
            DisputeId,
            ContractId,
            "CTR-2026-VIP01",
            "Tập đoàn Đất Vàng",
            "Xưởng Bánh Kinh Đô",
            "Giao trễ tiến độ",
            ["https://pod.jpg"],
            "Pending",
            null,
            null,
            0,
            35000000m,
            DateTime.UtcNow,
            null)
    };

    contractService.Setup(s => s.ListDisputesAsync(null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(disputes);

    var response = await client.GetAsync("/api/v1/admin/disputes");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("CTR-2026-VIP01", json.GetProperty("data")[0].GetProperty("contractNumber").GetString());
  }

  [Fact]
  public async Task AdminDisputes_List_Returns403Forbidden_ForCustomer()
  {
    var client = CreateClient("Customer");

    var response = await client.GetAsync("/api/v1/admin/disputes");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task AdminDisputes_ResolveById_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new AdminResolveDisputeRequest
    {
      ResolutionType = "SupplierAtFault",
      RefundAmount = 35000000m,
      AdminVerdict = "Căn cứ biên bản giao hàng trễ, hoàn trả 100% cọc."
    };

    var resolved = new ContractDisputeResponse(
        DisputeId,
        ContractId,
        CustomerUserId,
        "Giao trễ tiến độ",
        ["https://pod.jpg"],
        "Resolved",
        "Căn cứ biên bản giao hàng trễ, hoàn trả 100% cọc.",
        "SupplierAtFault",
        35000000m,
        DateTime.UtcNow.AddDays(-2),
        DateTime.UtcNow);

    contractService.Setup(s => s.ResolveDisputeByIdAsync(AdminUserId, DisputeId, It.IsAny<AdminResolveDisputeRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(resolved);

    var response = await client.PostAsJsonAsync($"/api/v1/admin/disputes/{DisputeId}/resolve", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("Resolved", json.GetProperty("data").GetProperty("status").GetString());
  }

  [Fact]
  public async Task AdminContracts_ResolveDispute_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new AdminResolveDisputeRequest
    {
      ResolutionType = "MutualAgreement",
      RefundAmount = 15000000m,
      AdminVerdict = "Hai bên đồng ý chia sẻ chi phí phát sinh."
    };

    var resolved = new ContractDisputeResponse(
        DisputeId,
        ContractId,
        CustomerUserId,
        "Khác biệt quy cách",
        null,
        "Resolved",
        "Hai bên đồng ý chia sẻ chi phí phát sinh.",
        "MutualAgreement",
        15000000m,
        DateTime.UtcNow.AddDays(-3),
        DateTime.UtcNow);

    contractService.Setup(s => s.ResolveDisputeAsync(AdminUserId, ContractId, It.IsAny<AdminResolveDisputeRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(resolved);

    var response = await client.PostAsJsonAsync($"/api/v1/admin/contracts/{ContractId}/resolve-dispute", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task AdminDisputes_Resolve_Returns403Forbidden_ForCustomer()
  {
    var client = CreateClient("Customer");

    var request = new AdminResolveDisputeRequest
    {
      ResolutionType = "SupplierAtFault",
      RefundAmount = 35000000m,
      AdminVerdict = "Không có quyền phán quyết."
    };

    var response = await client.PostAsJsonAsync($"/api/v1/admin/disputes/{DisputeId}/resolve", request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // =========================================================================
  // FEATURE #27: QUẢN TRỊ NỀN TẢNG (ADMIN DASHBOARD, CONFIGS & USERS)
  // =========================================================================

  [Fact]
  public async Task AdminDashboard_Get_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var dashboard = new AdminDashboardResponse(
        TotalUsers: 200,
        ActiveUsers: 180,
        PendingSuppliers: 12,
        ActiveShops: 50,
        TotalOrders: 420,
        PaidGmv: 6500000000m,
        PendingPayments: 5,
        DisputedContracts: 1,
        TopSuppliers: [new AdminTopSupplierResponse(SupplierId, "Tràng An Bakery", 2000000000m)],
        TopProducts: [new AdminTopProductResponse(Guid.NewGuid(), "Hộp VIP Long Phụng", 500, 750000000m)]);

    adminPlatformService.Setup(s => s.GetDashboardAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(dashboard);

    var response = await client.GetAsync("/api/v1/admin/dashboard");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(200, json.GetProperty("data").GetProperty("totalUsers").GetInt32());
    Assert.Equal(6500000000m, json.GetProperty("data").GetProperty("paidGmv").GetDecimal());
  }

  [Fact]
  public async Task AdminDashboard_Get_Returns403Forbidden_ForCustomer()
  {
    var client = CreateClient("Customer");

    var response = await client.GetAsync("/api/v1/admin/dashboard");

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task AdminSystemConfig_Get_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var configs = new List<SystemConfigResponse>
    {
        new("escrow_period_days", "{\"days\": 7}", "Thời gian giữ cọc Escrow", DateTime.UtcNow)
    };

    adminPlatformService.Setup(s => s.GetSystemConfigsAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(configs);

    var response = await client.GetAsync("/api/v1/admin/system-config");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("escrow_period_days", json.GetProperty("data")[0].GetProperty("key").GetString());
  }

  [Fact]
  public async Task AdminSystemConfig_Set_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new SetSystemConfigRequest("{\"days\": 14}", "Tăng thời gian giữ cọc");
    var updated = new SystemConfigResponse("escrow_period_days", "{\"days\": 14}", "Tăng thời gian giữ cọc", DateTime.UtcNow);

    adminPlatformService.Setup(s => s.SetSystemConfigAsync("escrow_period_days", "{\"days\": 14}", "Tăng thời gian giữ cọc", AdminUserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(updated);

    var response = await client.PutAsJsonAsync("/api/v1/admin/system-config/escrow_period_days", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("escrow_period_days", json.GetProperty("data").GetProperty("key").GetString());
  }

  [Fact]
  public async Task AdminAuditLogs_Get_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var logs = new List<AuditLogResponse>
    {
        new(Guid.NewGuid(), AdminUserId, "DisputeResolved", "ContractDispute", DisputeId.ToString(), null, "127.0.0.1", DateTime.UtcNow)
    };

    adminPlatformService.Setup(s => s.GetAuditLogsAsync(1, 50, It.IsAny<CancellationToken>()))
        .ReturnsAsync(logs);

    var response = await client.GetAsync("/api/v1/admin/audit-logs?page=1&pageSize=50");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("DisputeResolved", json.GetProperty("data")[0].GetProperty("action").GetString());
  }

  [Fact]
  public async Task AdminContractDepositRules_Get_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var rules = new List<ContractDepositRuleResponse>
    {
        new(DepositRuleId, "Standard 30%", 30m, true, 20000000m, 48)
    };

    adminPlatformService.Setup(s => s.GetContractDepositRulesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(rules);

    var response = await client.GetAsync("/api/v1/admin/contract-deposit-rules");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal(30m, json.GetProperty("data")[0].GetProperty("depositPercentage").GetDecimal());
  }

  [Fact]
  public async Task AdminContractDepositRules_Create_Returns201Created_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new SaveContractDepositRuleRequest
    {
      Name = "VIP Rule 50%",
      DepositPercentage = 50m,
      IsMandatory = true,
      MinContractValue = 100000000m,
      PaymentDeadlineHours = 72
    };

    var created = new ContractDepositRuleResponse(DepositRuleId, "VIP Rule 50%", 50m, true, 100000000m, 72);

    adminPlatformService.Setup(s => s.SaveContractDepositRuleAsync(null, It.IsAny<SaveContractDepositRuleRequest>(), AdminUserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(created);

    var response = await client.PostAsJsonAsync("/api/v1/admin/contract-deposit-rules", request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
  }

  [Fact]
  public async Task AdminContractDepositRules_Update_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new SaveContractDepositRuleRequest
    {
      Name = "Updated Rule",
      DepositPercentage = 35m,
      IsMandatory = true,
      PaymentDeadlineHours = 48
    };

    var updated = new ContractDepositRuleResponse(DepositRuleId, "Updated Rule", 35m, true, null, 48);

    adminPlatformService.Setup(s => s.SaveContractDepositRuleAsync(DepositRuleId, It.IsAny<SaveContractDepositRuleRequest>(), AdminUserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(updated);

    var response = await client.PutAsJsonAsync($"/api/v1/admin/contract-deposit-rules/{DepositRuleId}", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task AdminUsers_Update_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var request = new AdminUserUpdateRequest(IsActive: false, Role: null);

    adminPlatformService.Setup(s => s.UpdateUserAsync(TargetUserId, request, AdminUserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var response = await client.PatchAsJsonAsync($"/api/v1/admin/users/{TargetUserId}", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task AdminUsers_Update_Returns403Forbidden_ForCustomer()
  {
    var client = CreateClient("Customer");

    var request = new AdminUserUpdateRequest(IsActive: false, Role: null);
    var response = await client.PatchAsJsonAsync($"/api/v1/admin/users/{TargetUserId}", request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  // =========================================================================
  // FEATURE #27: QUẢN TRỊ GIAN HÀNG (SHOP MODERATION & LISTING)
  // =========================================================================

  [Fact]
  public async Task AdminShops_List_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var pageResponse = new ShopPageResponse<AdminShopSummaryResponse>(
        Items: [
            new AdminShopSummaryResponse(
                ShopId,
                SupplierId,
                "Bảo Ngọc Mooncake",
                "bao-ngoc-mooncake",
                ShopStatus.Active,
                1,
                15,
                DateTime.UtcNow)
        ],
        Page: 1,
        PageSize: 20,
        TotalCount: 1);

    shopService.Setup(s => s.ListAdminAsync(AdminUserId, It.IsAny<AdminShopQuery>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(pageResponse);

    var response = await client.GetAsync("/api/v1/admin/shops");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("Bảo Ngọc Mooncake", json.GetProperty("data").GetProperty("items")[0].GetProperty("name").GetString());
  }

  [Fact]
  public async Task AdminShops_Detail_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");

    var appearance = new ShopAppearanceResponse(
        Guid.NewGuid(), Guid.NewGuid(), "heritage", "Heritage Theme", "heritage", 1, 1, true, false,
        new ThemeOverridesRequest(), new ThemeOverridesRequest(), new ThemeOverridesRequest(), new TemplateLayoutRequest(), 1);

    var shop = new OwnerShopResponse(
        ShopId, SupplierId, 1, "Bảo Ngọc Mooncake", "bao-ngoc-mooncake", "Mô tả", ShopStatus.Active,
        null, null, null, null, "baongoc@gmail.com", "0987654321", "Hà Nội", "HN", null, appearance, ["Update"]);

    shopService.Setup(s => s.DetailAdminAsync(AdminUserId, ShopId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(shop);

    var response = await client.GetAsync($"/api/v1/admin/shops/{ShopId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task AdminShops_Moderate_Suspend_Returns200OK_ForAdmin()
  {
    var client = CreateClient("Admin");
    client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

    var request = new AdminShopModerationRequest
    {
      Action = "Suspend",
      ExpectedVersion = 1,
      Reason = "Gian hàng vi phạm hợp đồng B2B."
    };

    var appearance = new ShopAppearanceResponse(
        Guid.NewGuid(), Guid.NewGuid(), "heritage", "Heritage Theme", "heritage", 2, 1, true, false,
        new ThemeOverridesRequest(), new ThemeOverridesRequest(), new ThemeOverridesRequest(), new TemplateLayoutRequest(), 2);

    var suspended = new OwnerShopResponse(
        ShopId, SupplierId, 2, "Bảo Ngọc Mooncake", "bao-ngoc-mooncake", "Mô tả", ShopStatus.Suspended,
        null, null, null, null, "baongoc@gmail.com", "0987654321", "Hà Nội", "HN", null, appearance, ["Update"]);

    shopService.Setup(s => s.ModerateAsync(AdminUserId, ShopId, It.IsAny<string>(), It.IsAny<AdminShopModerationRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(suspended);

    var response = await client.PatchAsJsonAsync($"/api/v1/admin/shops/{ShopId}/status", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("Suspended", json.GetProperty("data").GetProperty("status").GetString());
  }

  [Fact]
  public async Task AdminShops_Moderate_Returns403Forbidden_ForCustomer()
  {
    var client = CreateClient("Customer");

    var request = new AdminShopModerationRequest
    {
      Action = "Suspend",
      ExpectedVersion = 1,
      Reason = "Customer cannot moderate shops."
    };

    var response = await client.PatchAsJsonAsync($"/api/v1/admin/shops/{ShopId}/status", request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
