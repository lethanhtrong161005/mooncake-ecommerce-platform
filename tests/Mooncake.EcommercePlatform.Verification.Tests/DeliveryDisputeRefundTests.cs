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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Files.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Npgsql;
using Npgsql.NameTranslation;
using Pgvector.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

/// <summary>Unit and PostgreSQL integration tests for PoD delivery dispute resolution, customer bank snapshots, and manual refund proof uploads.</summary>
public sealed class DeliveryDisputeRefundTests(ITestOutputHelper output)
{
    private const string SampleEvidenceUrl = "https://res.cloudinary.com/demo/image/upload/v1/disputes/damaged_boxes.jpg";
    private const string SampleTransferProofUrl = "https://res.cloudinary.com/demo/image/upload/v1/refunds/receipt_vcb_001.jpg";

    private static readonly Lazy<DbContextOptions<ApplicationDbContext>> SharedOptions = new(() =>
    {
        string? settingsPath = null;
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Mooncake.EcommercePlatform.WebApi", "appsettings.json");
            if (File.Exists(candidate))
            {
                settingsPath = candidate;
                break;
            }
            directory = directory.Parent;
        }

        var configurationBuilder = new ConfigurationBuilder();
        if (settingsPath is not null)
            configurationBuilder.AddJsonFile(settingsPath);

        var configuration = configurationBuilder.AddEnvironmentVariables().Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("Database connection string is missing.");

        var csb = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = 6
        };

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(csb.ConnectionString);
        dataSourceBuilder.UseVector();
        var translator = new NpgsqlNullNameTranslator();
        dataSourceBuilder.MapEnum<UserRole>("user_role", translator);
        dataSourceBuilder.MapEnum<ShopStatus>("shop_status", translator);
        dataSourceBuilder.MapEnum<ProductStatus>("product_status", translator);
        dataSourceBuilder.MapEnum<OrderStatus>("order_status", translator);
        dataSourceBuilder.MapEnum<PaymentMethod>("payment_method", translator);
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status", translator);
        dataSourceBuilder.MapEnum<DeliveryType>("delivery_type", translator);
        dataSourceBuilder.MapEnum<DeliveryStatus>("delivery_status", translator);
        dataSourceBuilder.MapEnum<ProofType>("proof_type", translator);
        dataSourceBuilder.MapEnum<CustomPackagingStatus>("custom_packaging_status", translator);
        dataSourceBuilder.MapEnum<RfqStatus>("rfq_status", translator);
        dataSourceBuilder.MapEnum<RfqInvitationStatus>("rfq_invitation_status", translator);
        dataSourceBuilder.MapEnum<BidStatus>("bid_status", translator);
        dataSourceBuilder.MapEnum<ContractStatus>("contract_status", translator);
        dataSourceBuilder.MapEnum<MilestoneStatus>("milestone_status", translator);
        dataSourceBuilder.MapEnum<DiscountType>("discount_type", translator);
        dataSourceBuilder.MapEnum<AiConversationType>("ai_conversation_type", translator);
        dataSourceBuilder.MapEnum<AiMessageRole>("ai_message_role", translator);
        dataSourceBuilder.MapEnum<AiReportType>("ai_report_type", translator);
        dataSourceBuilder.MapEnum<AiReportStatus>("ai_report_status", translator);
        dataSourceBuilder.MapEnum<ReputationEvent>("reputation_event", translator);

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSourceBuilder.Build(), npgsql => npgsql.UseVector())
            .Options;
    });

    private static ApplicationDbContext CreateDbContext() => new(SharedOptions.Value);

    private static (
        ApplicationDbContext Db,
        OrderService OrderService,
        DirectBulkOrderService EnterpriseService,
        DeliveryService DeliveryService,
        EnterpriseOrderRepository EnterpriseRepo,
        PaymentRepository PaymentRepo) CreateStack()
    {
        var db = CreateDbContext();
        var clock = new DateTimeProvider();
        var orderRepo = new OrderRepository(db, clock);
        var enterpriseRepo = new EnterpriseOrderRepository(db, clock);
        var paymentRepo = new PaymentRepository(db, clock);
        var deliveryRepo = new DeliveryRepository(db);
        var vnPayGateway = new VnPayGateway(Options.Create(new VnPayOptions
        {
            TmnCode = "MOONCAKE",
            HashSecret = "MOONCAKE_SANDBOX_SECRET_KEY_1234567890",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        }));

        var orderService = new OrderService(orderRepo, clock, enterpriseRepo);
        var enterpriseService = new DirectBulkOrderService(orderRepo, enterpriseRepo, vnPayGateway, clock);
        var deliveryService = new DeliveryService(deliveryRepo, clock);

        return (db, orderService, enterpriseService, deliveryService, enterpriseRepo, paymentRepo);
    }

    private sealed class CreatedOrderContext
    {
        public required Guid OrderId { get; init; }
        public required Guid DeliveryId { get; init; }
        public required Guid VariantId { get; init; }
        public required int OriginalStockQty { get; init; }
        public required decimal PaidAmount { get; init; }
    }

    private static IFormFile CreateFakeFormFile(string fileName, string content = "fake-image-bytes")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    [Fact]
    public async Task UploadRefundProofAsync_ValidFile_ReturnsCloudinaryUrl()
    {
        var enterpriseService = new Mock<IDirectBulkOrderService>();
        var cloudinary = new Mock<ICloudinaryStorageService>();
        cloudinary
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "bill.png", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResponse(SampleTransferProofUrl, "refunds/bill_01", "image", "upload", 1024L));

        var controller = new AdminOrderOperationsController(enterpriseService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.UploadRefundProofAsync(
            CreateFakeFormFile("bill.png"),
            cloudinary.Object,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains(SampleTransferProofUrl, json);
        Assert.Contains("refunds/bill_01", json);
        cloudinary.Verify(c => c.UploadImageAsync(It.IsAny<Stream>(), "bill.png", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadRefundProofAsync_EmptyFile_Throws400ValidationError()
    {
        var controller = new AdminOrderOperationsController(new Mock<IDirectBulkOrderService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            controller.UploadRefundProofAsync(null, new Mock<ICloudinaryStorageService>().Object, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
    }

    [Fact]
    public async Task UploadPayoutProofAsync_ValidFile_ReturnsCloudinaryUrl()
    {
        var cloudinary = new Mock<ICloudinaryStorageService>();
        cloudinary
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "payout-pod.png", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResponse(SampleTransferProofUrl, "payouts/pod_01", "image", "upload", 2048L));

        var controller = new AdminOrderOperationsController(new Mock<IDirectBulkOrderService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.UploadPayoutProofAsync(
            CreateFakeFormFile("payout-pod.png"),
            cloudinary.Object,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains(SampleTransferProofUrl, json);
        Assert.Contains("payouts/pod_01", json);
        cloudinary.Verify(c => c.UploadImageAsync(It.IsAny<Stream>(), "payout-pod.png", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadPayoutProofAsync_EmptyFile_Throws400ValidationError()
    {
        var controller = new AdminOrderOperationsController(new Mock<IDirectBulkOrderService>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var ex = await Assert.ThrowsAsync<HttpException>(() =>
            controller.UploadPayoutProofAsync(null, new Mock<ICloudinaryStorageService>().Object, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(OrderErrorCodes.ValidationError, ex.ErrorCode);
    }

    [Fact]
    public async Task UploadDisputeEvidenceAsync_ValidFile_ReturnsCloudinaryUrl()
    {
        var orderService = new Mock<IOrderService>();
        var cloudinary = new Mock<ICloudinaryStorageService>();
        cloudinary
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "broken.jpg", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResponse(SampleEvidenceUrl, "disputes/broken_01", "image", "upload", 1024L));

        var controller = new OrdersController(orderService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.UploadDisputeEvidenceAsync(
            CreateFakeFormFile("broken.jpg"),
            cloudinary.Object,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains(SampleEvidenceUrl, json);
        Assert.Contains("disputes/broken_01", json);
    }

    [Fact]
    public async Task HttpContract_RefundProofUploadAndDisputeResolutionEndpoints_BindPayloadsAndEnforceRolesAsync()
    {
        var customerId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var disputeId = Guid.NewGuid();
        var refundCaseId = Guid.NewGuid();
        var payoutRequestId = Guid.NewGuid();

        var orderService = new Mock<IOrderService>();
        var enterpriseService = new Mock<IDirectBulkOrderService>();
        var cloudinary = new Mock<ICloudinaryStorageService>();

        cloudinary
            .Setup(c => c.UploadImageAsync(It.IsAny<Stream>(), "bill.png", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResponse(SampleTransferProofUrl, "refunds/bill_http", "image", "upload", 512L));

        enterpriseService
            .Setup(s => s.ReportDeliveryDisputeAsync(customerId, orderId, It.IsAny<ReportOrderDeliveryDisputeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid oId, ReportOrderDeliveryDisputeRequest req, CancellationToken _) =>
                new OrderDeliveryDisputeResponse(
                    disputeId,
                    oId,
                    "MC-20261115-HTTP01",
                    req.DeliveryId,
                    req.IssueType,
                    req.Description,
                    req.EvidenceUrls ?? [],
                    "Open",
                    null,
                    DateTime.UtcNow,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        BankName = req.RefundBankName,
                        AccountNumber = req.RefundAccountNumber,
                        AccountHolderName = req.RefundAccountHolderName
                    }),
                    5_000_000m));

        enterpriseService
            .Setup(s => s.ResolveDeliveryDisputeAsync(adminId, disputeId, It.IsAny<ResolveOrderDeliveryDisputeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid dId, ResolveOrderDeliveryDisputeRequest req, CancellationToken _) =>
                new OrderDeliveryDisputeResponse(
                    dId,
                    orderId,
                    "MC-20261115-HTTP01",
                    null,
                    "DamagedGoods",
                    "Missing boxes",
                    [SampleEvidenceUrl],
                    "ResolvedRefund",
                    req.Notes,
                    DateTime.UtcNow.AddMinutes(-5),
                    DateTime.UtcNow,
                    "{\"BankName\":\"VCB\",\"AccountNumber\":\"0011223344\"}",
                    5_000_000m));

        enterpriseService
            .Setup(s => s.ProcessRefundCaseAsync(adminId, refundCaseId, It.IsAny<ProcessOrderRefundCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid rcId, ProcessOrderRefundCaseRequest req, CancellationToken _) =>
                new OrderRefundCaseResponse(
                    rcId,
                    orderId,
                    "MC-20261115-HTTP01",
                    "RFD-20261115-HTTP",
                    5_000_000m,
                    req.ApprovedAmount ?? 5_000_000m,
                    "Refund claim",
                    "CompletedSimulated",
                    false,
                    DateTime.UtcNow.AddMinutes(-5),
                    DateTime.UtcNow,
                    req.Notes,
                    req.RefundReferenceCode,
                    "{\"BankName\":\"VCB\",\"AccountNumber\":\"0011223344\"}",
                    req.TransferProofImageUrl));

        enterpriseService
            .Setup(s => s.CreatePayoutRequestAsync(supplierId, orderId, It.IsAny<CreateOrderPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid oId, CreateOrderPayoutRequest req, CancellationToken _) =>
                new SupplierPayoutRequestResponse(
                    payoutRequestId,
                    oId,
                    "MC-20261115-HTTP01",
                    $"PYT-{oId.ToString()[..6]}",
                    5_000_000m,
                    "Pending",
                    false,
                    JsonSerializer.Serialize(new
                    {
                        req.BankCode,
                        req.BankName,
                        req.AccountNumber,
                        req.AccountHolderName,
                        req.BranchName
                    }),
                    DateTime.UtcNow,
                    null,
                    req.Notes,
                    null,
                    null));

        enterpriseService
            .Setup(s => s.ProcessPayoutRequestAsync(adminId, payoutRequestId, It.IsAny<ProcessOrderPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid prId, ProcessOrderPayoutRequest req, CancellationToken _) =>
                new SupplierPayoutRequestResponse(
                    prId,
                    orderId,
                    "MC-20261115-HTTP01",
                    $"PYT-{orderId.ToString()[..6]}",
                    5_000_000m,
                    req.Approve ? "PaidOutSimulated" : "Rejected",
                    string.IsNullOrWhiteSpace(req.TransferProofImageUrl),
                    "{\"BankName\":\"ACB\",\"AccountNumber\":\"123456789012\"}",
                    DateTime.UtcNow.AddMinutes(-5),
                    DateTime.UtcNow,
                    req.Notes,
                    req.PayoutReferenceCode,
                    req.TransferProofImageUrl));

        async Task<HttpClient> CreateHttpClientAsync(string role, Guid userId)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(AdminOrderOperationsController).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton(orderService.Object);
            builder.Services.AddSingleton(enterpriseService.Object);
            builder.Services.AddSingleton(cloudinary.Object);
            builder.Services.AddSingleton(new Mock<IPromotionService>().Object);
            builder.Services.AddSingleton(new Mock<ICustomPackagingService>().Object);

            builder.Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            builder.Services.AddAuthorization(options =>
                options.AddPolicy("SupplierExistingFulfillment", policy => policy.RequireRole("Supplier")));

            var app = builder.Build();
            app.UseExceptionHandler();
            app.Use(async (context, next) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "Test"));
                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            return app.GetTestClient();
        }

        var customerClient = await CreateHttpClientAsync("Customer", customerId);
        var supplierClient = await CreateHttpClientAsync("Supplier", supplierId);
        var adminClient = await CreateHttpClientAsync("Admin", adminId);

        // 1. Role enforcement: Customer cannot upload admin refund proof or resolve disputes
        using (var form = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", "bill.png");

            var forbiddenRes = await customerClient.PostAsync("/api/v1/admin/orders/refund-proof/upload", form);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenRes.StatusCode);
        }

        // 2. Admin uploads manual transfer proof receipt -> 200 OK + url
        using (var form = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", "bill.png");

            var okUpload = await adminClient.PostAsync("/api/v1/admin/orders/refund-proof/upload", form);
            Assert.Equal(HttpStatusCode.OK, okUpload.StatusCode);
            var json = await okUpload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(SampleTransferProofUrl, json.GetProperty("data").GetProperty("url").GetString());
        }

        // 3. Customer submits PoD dispute with bank snapshot -> 200 OK
        var reportRes = await customerClient.PostAsJsonAsync($"/api/v1/orders/{orderId}/delivery/dispute", new
        {
            issueType = "DamagedGoods",
            description = "Hop banh bi be nat khi giao toi kho.",
            evidenceUrls = new[] { SampleEvidenceUrl },
            refundBankName = "Vietcombank",
            refundAccountNumber = "0071000888999",
            refundAccountHolderName = "NGUYEN VAN A"
        });
        Assert.Equal(HttpStatusCode.OK, reportRes.StatusCode);
        var reportJson = await reportRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5_000_000m, reportJson.GetProperty("data").GetProperty("orderPaidAmount").GetDecimal());
        Assert.Contains("Vietcombank", reportJson.GetProperty("data").GetProperty("refundBankSnapshotJson").GetString());

        // 4. Admin resolves PoD dispute with immediate proof image & ref code -> 200 OK
        var resolveRes = await adminClient.PostAsJsonAsync($"/api/v1/admin/orders/delivery-disputes/{disputeId}/resolve", new
        {
            resolutionAction = "FullRefund",
            refundAmount = 0,
            notes = "Chap thuan hoan 100% qua chuyen khoan.",
            deferTransfer = false,
            refundReferenceCode = "TRF-VCB-999",
            transferProofImageUrl = SampleTransferProofUrl
        });
        Assert.Equal(HttpStatusCode.OK, resolveRes.StatusCode);
        var resolveJson = await resolveRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ResolvedRefund", resolveJson.GetProperty("data").GetProperty("status").GetString());

        // 5. Admin completes 2-step refund case with proof URL -> 200 OK
        var processRes = await adminClient.PostAsJsonAsync($"/api/v1/admin/orders/refund-cases/{refundCaseId}/process", new
        {
            approve = true,
            approvedAmount = 5_000_000m,
            refundReferenceCode = "TRF-VCB-1000",
            transferProofImageUrl = SampleTransferProofUrl,
            notes = "Da chuyen khoan hoan tien thanh cong."
        });
        Assert.Equal(HttpStatusCode.OK, processRes.StatusCode);
        var processJson = await processRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CompletedSimulated", processJson.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(SampleTransferProofUrl, processJson.GetProperty("data").GetProperty("transferProofImageUrl").GetString());

        // 6. Payout proof upload role check & supplier inline bank payout + admin proof submission
        using (var form = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", "bill.png");

            var forbiddenPayoutUpload = await supplierClient.PostAsync("/api/v1/admin/orders/payout-proof/upload", form);
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenPayoutUpload.StatusCode);
        }

        using (var form = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", "bill.png");

            var okPayoutUpload = await adminClient.PostAsync("/api/v1/admin/orders/payout-proof/upload", form);
            Assert.Equal(HttpStatusCode.OK, okPayoutUpload.StatusCode);
        }

        var createPayoutRes = await supplierClient.PostAsJsonAsync($"/api/v1/orders/{orderId}/payout-requests", new
        {
            bankCode = "ACB",
            bankName = "Ngân hàng Á Châu ACB",
            accountNumber = "123456789012",
            accountHolderName = "XUONG BANH KIM LONG",
            branchName = "Chi nhanh Q1",
            notes = "Giai ngan don hoan tat"
        });
        Assert.Equal(HttpStatusCode.OK, createPayoutRes.StatusCode);
        var createPayoutJson = await createPayoutRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("123456789012", createPayoutJson.GetProperty("data").GetProperty("bankSnapshotJson").GetString());

        var processPayoutRes = await adminClient.PostAsJsonAsync($"/api/v1/admin/orders/payout-requests/{payoutRequestId}/process", new
        {
            approve = true,
            payoutReferenceCode = "TRF-PYT-ACB-001",
            transferProofImageUrl = SampleTransferProofUrl,
            notes = "Da chuyen khoan cho xuong va up PoD."
        });
        Assert.Equal(HttpStatusCode.OK, processPayoutRes.StatusCode);
        var processPayoutJson = await processPayoutRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PaidOutSimulated", processPayoutJson.GetProperty("data").GetProperty("status").GetString());
        Assert.False(processPayoutJson.GetProperty("data").GetProperty("sandboxSimulated").GetBoolean());
        Assert.Equal(SampleTransferProofUrl, processPayoutJson.GetProperty("data").GetProperty("transferProofImageUrl").GetString());
    }

    [Fact]
    public async Task DeliveryDisputeAndRefundWorkflow_FullImmediate_Deferred_Partial_And_ReleasePayout_PostgreSql()
    {
        NpgsqlConnection.ClearAllPools();

        await using var lookupDb = CreateDbContext();
        var custA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "cust.a@test.mooncake.local");
        var supA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "sup.a@test.mooncake.local");
        var admin = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "admin@test.mooncake.local");

        var shop = await (from s in lookupDb.Shops.AsNoTracking()
                          join p in lookupDb.SupplierProfiles.AsNoTracking() on s.SupplierId equals p.Id into profiles
                          from p in profiles.DefaultIfEmpty()
                          where !s.IsDeleted && s.Status == ShopStatus.Active
                                && (s.OwnerUserId == supA.Id || (p != null && p.UserId == supA.Id && !p.IsDeleted))
                          orderby s.CreatedAt
                          select s).FirstAsync();

        var shippingZoneCode = await lookupDb.ShopShippingRates.AsNoTracking()
            .Where(r => r.ShopId == shop.Id && !r.IsDeleted)
            .Select(r => r.RegionCode)
            .FirstOrDefaultAsync() ?? "HCM_INNER";

        var productAndVariant = await (from v in lookupDb.ProductVariants.AsNoTracking()
                                       join p in lookupDb.Products.AsNoTracking() on v.ProductId equals p.Id
                                       where p.ShopId == shop.Id
                                             && !p.IsDeleted
                                             && !v.IsDeleted
                                             && v.StockQty >= 50
                                             && p.BasePrice > 0m
                                       orderby v.CreatedAt
                                       select new { Product = p, Variant = v }).FirstAsync();

        var qty = Math.Max(productAndVariant.Product.MinOrderQty, 20);
        if (productAndVariant.Variant.StockQty < qty * 4)
        {
            await lookupDb.ProductVariants
                .Where(v => v.Id == productAndVariant.Variant.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, qty * 4 + 20));
        }
        var currentStockSnapshot = await lookupDb.ProductVariants.AsNoTracking()
            .Where(v => v.Id == productAndVariant.Variant.Id)
            .Select(v => v.StockQty)
            .SingleAsync();

        var createdOrderIds = new List<Guid>();

        async Task<CreatedOrderContext> CreatePaidShippedPodOrderAsync()
        {
            var st = CreateStack();
            await using var _ = st.Db;

            var deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var quote = await st.OrderService.PreviewQuoteAsync(new OrderQuotePreviewRequest
            {
                ShopId = shop.Id,
                ShippingZoneCode = shippingZoneCode,
                Items = [new CreateOrderItemRequest
                {
                    ProductId = productAndVariant.Product.Id,
                    VariantId = productAndVariant.Variant.Id,
                    Quantity = qty
                }]
            });

            var order = await st.OrderService.CreateAsync(custA.Id, Guid.NewGuid().ToString("N"), new CreateOrderRequest
            {
                ShopId = shop.Id,
                DeliveryAddress = "101 Le Loi, Quan 1, TP. Ho Chi Minh",
                RecipientName = "Cong ty Thuong Mai Anh Trang",
                RecipientPhone = "0988000101",
                ShippingZoneCode = shippingZoneCode,
                DeliveryDateExpected = deliveryDate,
                AcceptedPriceVersion = quote.PriceVersion,
                AcceptedTerms = true,
                Items = [new CreateOrderItemRequest
                {
                    ProductId = productAndVariant.Product.Id,
                    VariantId = productAndVariant.Variant.Id,
                    Quantity = qty
                }]
            });
            createdOrderIds.Add(order.Id);

            var accepted = await st.OrderService.ConfirmAsync(supA.Id, order.Id);
            Assert.Equal(OrderStatus.Confirmed, accepted.Status);

            var summary = await st.EnterpriseService.GetPaymentSummaryAsync(custA.Id, false, false, order.Id);
            foreach (var part in summary.Obligations.SelectMany(o => o.Parts).Where(p => p.Status != "Paid"))
            {
                var vnPay = await st.EnterpriseService.CreatePartVnPayUrlAsync(custA.Id, order.Id, part.Id,
                    new CreateOrderPartVnPayUrlRequest { ReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua" },
                    "127.0.0.1");
                var payStack = CreateStack();
                await using var __ = payStack.Db;
                await payStack.PaymentRepo.ConfirmVnPayPaymentAsync(vnPay.PaymentNumber);
            }

            await st.OrderService.StartProcessingAsync(supA.Id, order.Id);
            await st.OrderService.MarkShippedAsync(supA.Id, order.Id);

            var shipment = await st.DeliveryService.CreateAsync(supA.Id, new CreateDeliveryRequest
            {
                OrderId = order.Id,
                DeliveryType = DeliveryType.SelfArranged,
                ScheduledDate = deliveryDate,
                FromAddress = shop.PublicAddress ?? "TP. Ho Chi Minh",
                ToAddress = "101 Le Loi, Quan 1, TP. Ho Chi Minh",
                RecipientName = "Cong ty Thuong Mai Anh Trang",
                RecipientPhone = "0988000101",
                TrackingCode = $"TRK-TEST-{Guid.NewGuid():N}"[..16]
            });

            await st.DeliveryService.SubmitProofAsync(supA.Id, shipment.Id, new CreateDeliveryProofRequest
            {
                ProofType = ProofType.Delivery,
                PhotoUrl = SampleEvidenceUrl,
                Notes = "PoD proof for automated refund verification"
            });

            var paidFin = await st.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, order.Id);
            return new CreatedOrderContext
            {
                OrderId = order.Id,
                DeliveryId = shipment.Id,
                VariantId = productAndVariant.Variant.Id,
                OriginalStockQty = currentStockSnapshot,
                PaidAmount = paidFin.CapturedAmount
            };
        }

        try
        {
            // =====================================================================
            // SCENARIO 1: FullRefund Immediate with Customer Bank + Proof Image
            // =====================================================================
            var ctx1 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                var dispute = await st.EnterpriseService.ReportDeliveryDisputeAsync(custA.Id, ctx1.OrderId,
                    new ReportOrderDeliveryDisputeRequest
                    {
                        DeliveryId = ctx1.DeliveryId,
                        IssueType = "DamagedGoods",
                        Description = "Thung banh bi vo nat do am uot trong qua trinh van chuyen.",
                        EvidenceUrls = [SampleEvidenceUrl],
                        RefundBankName = "Vietcombank",
                        RefundAccountNumber = "0071000888999",
                        RefundAccountHolderName = "NGUYEN VAN A"
                    });

                Assert.Equal("Open", dispute.Status);
                Assert.Equal(ctx1.PaidAmount, dispute.OrderPaidAmount);
                Assert.NotNull(dispute.RefundBankSnapshotJson);
                Assert.Contains("Vietcombank", dispute.RefundBankSnapshotJson);
                Assert.Contains("0071000888999", dispute.RefundBankSnapshotJson);

                // Validation guard checks
                await Assert.ThrowsAsync<HttpException>(() =>
                    st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                        new ResolveOrderDeliveryDisputeRequest
                        {
                            ResolutionAction = "UnknownAction",
                            Notes = "Invalid action check"
                        }));

                await Assert.ThrowsAsync<HttpException>(() =>
                    st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                        new ResolveOrderDeliveryDisputeRequest
                        {
                            ResolutionAction = "PartialRefund",
                            RefundAmount = 0m,
                            Notes = "Zero partial amount check"
                        }));

                // Resolve FullRefund immediately with transfer proof URL & ref code
                var resolved = await st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                    new ResolveOrderDeliveryDisputeRequest
                    {
                        ResolutionAction = "FullRefund",
                        Notes = "Chap nhan hoan tien 100% va da chuyen khoan qua VCB.",
                        DeferTransfer = false,
                        RefundReferenceCode = "TRF-VCB-TEST-001",
                        TransferProofImageUrl = SampleTransferProofUrl
                    });

                Assert.Equal("ResolvedRefund", resolved.Status);

                var fin = await st.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, ctx1.OrderId);
                Assert.Equal(0m, fin.HeldAmount);
                Assert.Equal(0m, fin.DisputedLockedAmount);
                Assert.Equal(0m, fin.EligibleAmount);
                Assert.Equal(ctx1.PaidAmount, fin.RefundedSimulatedAmount);

                var orderEntity = await st.Db.Orders.AsNoTracking().SingleAsync(o => o.Id == ctx1.OrderId);
                Assert.Equal(OrderStatus.Cancelled, orderEntity.Status);

                var deliveryEntity = await st.Db.Deliveries.AsNoTracking().SingleAsync(d => d.Id == ctx1.DeliveryId);
                Assert.Equal(DeliveryStatus.Returned, deliveryEntity.Status);

                var refundCase = await st.Db.OrderRefundCases.AsNoTracking().SingleAsync(r => r.OrderId == ctx1.OrderId);
                Assert.Equal("CompletedSimulated", refundCase.Status);
                Assert.Equal(ctx1.PaidAmount, refundCase.ApprovedAmount);
                Assert.Equal("TRF-VCB-TEST-001", refundCase.RefundReferenceCode);
                Assert.Equal(SampleTransferProofUrl, refundCase.TransferProofImageUrl);
                Assert.Contains("0071000888999", refundCase.CustomerBankSnapshotJson);

                output.WriteLine($"[PASS] Scenario 1 FullRefund Immediate: Order={ctx1.OrderId}, Paid={ctx1.PaidAmount:N0}");
            }

            // =====================================================================
            // SCENARIO 2: FullRefund Deferred (2-Step) -> Process with Proof Image
            // =====================================================================
            var ctx2 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                var dispute = await st.EnterpriseService.ReportDeliveryDisputeAsync(custA.Id, ctx2.OrderId,
                    new ReportOrderDeliveryDisputeRequest
                    {
                        DeliveryId = ctx2.DeliveryId,
                        IssueType = "Shortage",
                        Description = "Thieu 10 hop banh thuong hang theo bien ban kiem dem.",
                        RefundBankName = "Techcombank",
                        RefundAccountNumber = "19033344455566",
                        RefundAccountHolderName = "TRAN THI B"
                    });

                var resolved = await st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                    new ResolveOrderDeliveryDisputeRequest
                    {
                        ResolutionAction = "FullRefund",
                        Notes = "Duyet hoan 100%, chuyen sang tab Ho So Hoan Tien de chuyen khoan sau.",
                        DeferTransfer = true
                    });
                Assert.Equal("ResolvedRefund", resolved.Status);

                // Funds must remain Held (NOT unlocked to Eligible, NOT Refunded yet)
                var midFin = await st.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, ctx2.OrderId);
                Assert.Equal(ctx2.PaidAmount, midFin.HeldAmount);
                Assert.Equal(0m, midFin.DisputedLockedAmount);
                Assert.Equal(0m, midFin.EligibleAmount);
                Assert.Equal(0m, midFin.RefundedSimulatedAmount);

                var pendingRefund = await st.Db.OrderRefundCases.AsNoTracking().SingleAsync(r => r.OrderId == ctx2.OrderId);
                Assert.Equal("Pending", pendingRefund.Status);
                Assert.Contains("19033344455566", pendingRefund.CustomerBankSnapshotJson);

                // Admin processes pending refund case with proof URL
                var processed = await st.EnterpriseService.ProcessRefundCaseAsync(admin.Id, pendingRefund.Id,
                    new ProcessOrderRefundCaseRequest
                    {
                        Approve = true,
                        ApprovedAmount = ctx2.PaidAmount,
                        RefundReferenceCode = "TRF-TCB-TEST-002",
                        TransferProofImageUrl = SampleTransferProofUrl,
                        Notes = "Da chuyen khoan TCB cho khach va luu chung tu."
                    });

                Assert.Equal("CompletedSimulated", processed.Status);
                Assert.Equal(SampleTransferProofUrl, processed.TransferProofImageUrl);
                Assert.Equal("TRF-TCB-TEST-002", processed.RefundReferenceCode);

                var endFin = await st.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, ctx2.OrderId);
                Assert.Equal(0m, endFin.HeldAmount);
                Assert.Equal(0m, endFin.EligibleAmount);
                Assert.Equal(ctx2.PaidAmount, endFin.RefundedSimulatedAmount);

                output.WriteLine($"[PASS] Scenario 2 FullRefund 2-Step Deferred: RefundCase={processed.CaseNumber}");
            }

            // =====================================================================
            // SCENARIO 3: PartialRefund Deferred -> Remainder Eligible Unlocked Only
            // =====================================================================
            var ctx3 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                var partialRefundAmt = decimal.Round(ctx3.PaidAmount * 0.25m, 0, MidpointRounding.AwayFromZero);
                var expectedSupplierRemainder = ctx3.PaidAmount - partialRefundAmt;

                var dispute = await st.EnterpriseService.ReportDeliveryDisputeAsync(custA.Id, ctx3.OrderId,
                    new ReportOrderDeliveryDisputeRequest
                    {
                        DeliveryId = ctx3.DeliveryId,
                        IssueType = "DamagedGoods",
                        Description = "Hu hai hop ben ngoai khoang 25% gia tri don hang.",
                        RefundBankName = "ACB",
                        RefundAccountNumber = "8899001122",
                        RefundAccountHolderName = "LE VAN C"
                    });

                var resolved = await st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                    new ResolveOrderDeliveryDisputeRequest
                    {
                        ResolutionAction = "PartialRefund",
                        RefundAmount = partialRefundAmt,
                        DeferTransfer = true,
                        Notes = "Hoa giai hoan 25% cho khach, phan con lai giai phong cho NCC."
                    });
                Assert.Equal("ResolvedRefund", resolved.Status);

                // Supplier remainder unlocked to Eligible, pending refund stays Held
                var midFin = await st.EnterpriseService.GetFinancialSummaryAsync(supA.Id, true, false, ctx3.OrderId);
                Assert.Equal(0m, midFin.DisputedLockedAmount);
                Assert.Equal(partialRefundAmt, midFin.HeldAmount);
                Assert.Equal(expectedSupplierRemainder, midFin.EligibleAmount);
                Assert.Equal(0m, midFin.RefundedSimulatedAmount);

                var pendingRefund = await st.Db.OrderRefundCases.AsNoTracking().SingleAsync(r => r.OrderId == ctx3.OrderId);
                var processed = await st.EnterpriseService.ProcessRefundCaseAsync(admin.Id, pendingRefund.Id,
                    new ProcessOrderRefundCaseRequest
                    {
                        Approve = true,
                        ApprovedAmount = partialRefundAmt,
                        RefundReferenceCode = "TRF-ACB-TEST-003",
                        TransferProofImageUrl = SampleTransferProofUrl,
                        Notes = "Hoan tat chuyen khoan hoa giai 25%."
                    });
                Assert.Equal("CompletedSimulated", processed.Status);

                var endFin = await st.EnterpriseService.GetFinancialSummaryAsync(supA.Id, true, false, ctx3.OrderId);
                Assert.Equal(0m, endFin.HeldAmount);
                Assert.Equal(expectedSupplierRemainder, endFin.EligibleAmount);
                Assert.Equal(partialRefundAmt, endFin.RefundedSimulatedAmount);

                output.WriteLine($"[PASS] Scenario 3 PartialRefund Deferred: Refund={partialRefundAmt:N0}, Eligible={expectedSupplierRemainder:N0}");
            }

            // =====================================================================
            // SCENARIO 4: ReleasePayout -> Unlocks Held to Eligible
            // =====================================================================
            var ctx4 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                var dispute = await st.EnterpriseService.ReportDeliveryDisputeAsync(custA.Id, ctx4.OrderId,
                    new ReportOrderDeliveryDisputeRequest
                    {
                        DeliveryId = ctx4.DeliveryId,
                        IssueType = "WrongItems",
                        Description = "Kiem tra nham nhan hop banh, sau do xac nhan dung chung loai."
                    });

                var resolved = await st.EnterpriseService.ResolveDeliveryDisputeAsync(admin.Id, dispute.Id,
                    new ResolveOrderDeliveryDisputeRequest
                    {
                        ResolutionAction = "ReleasePayout",
                        Notes = "PoD hop le, giai phong thanh toan cho NCC."
                    });
                Assert.Equal("ResolvedReleasePayout", resolved.Status);

                var fin = await st.EnterpriseService.GetFinancialSummaryAsync(supA.Id, true, false, ctx4.OrderId);
                Assert.Equal(0m, fin.HeldAmount);
                Assert.Equal(0m, fin.DisputedLockedAmount);
                Assert.Equal(ctx4.PaidAmount, fin.EligibleAmount);
                Assert.Equal(0m, fin.RefundedSimulatedAmount);

                output.WriteLine($"[PASS] Scenario 4 ReleasePayout: Eligible={fin.EligibleAmount:N0}");
            }

            // =====================================================================
            // SCENARIO 5: RequestRefundAsync (Order Disputed/Cancelled) + Bank Snapshot
            // =====================================================================
            var ctx5 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                await st.EnterpriseService.ReportDeliveryDisputeAsync(custA.Id, ctx5.OrderId,
                    new ReportOrderDeliveryDisputeRequest
                    {
                        DeliveryId = ctx5.DeliveryId,
                        IssueType = "DamagedGoods",
                        Description = "Khach hang gui yeu cau hoan tien truc tiep sau tranh chap.",
                    });

                var refundReq = await st.EnterpriseService.RequestRefundAsync(custA.Id, ctx5.OrderId,
                    new CreateOrderRefundRequest
                    {
                        RequestedAmount = ctx5.PaidAmount,
                        Reason = "Yeu cau hoan tien day du qua STK MB Bank.",
                        RefundBankName = "MB Bank",
                        RefundAccountNumber = "098877665544",
                        RefundAccountHolderName = "PHAM MINH D"
                    });

                Assert.Equal("Pending", refundReq.Status);
                Assert.NotNull(refundReq.CustomerBankSnapshotJson);
                Assert.Contains("MB Bank", refundReq.CustomerBankSnapshotJson);
                Assert.Contains("098877665544", refundReq.CustomerBankSnapshotJson);

                var processed = await st.EnterpriseService.ProcessRefundCaseAsync(admin.Id, refundReq.Id,
                    new ProcessOrderRefundCaseRequest
                    {
                        Approve = true,
                        ApprovedAmount = ctx5.PaidAmount,
                        RefundReferenceCode = "TRF-MB-TEST-005",
                        TransferProofImageUrl = SampleTransferProofUrl,
                        Notes = "Da chuyen khoan MB Bank va dinh kem anh bill."
                    });

                Assert.Equal("CompletedSimulated", processed.Status);
                Assert.Equal(SampleTransferProofUrl, processed.TransferProofImageUrl);
                Assert.Contains("098877665544", processed.CustomerBankSnapshotJson);

                var endFin = await st.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, ctx5.OrderId);
                Assert.Equal(ctx5.PaidAmount, endFin.RefundedSimulatedAmount);
                Assert.Equal(0m, endFin.HeldAmount);

                output.WriteLine($"[PASS] Scenario 5 Manual RequestRefundAsync + Proof: RefundCase={processed.CaseNumber}");
            }

            // =====================================================================
            // SCENARIO 6: Supplier Inline Bank Payout + Admin Manual Transfer Proof
            // =====================================================================
            var ctx6 = await CreatePaidShippedPodOrderAsync();
            {
                var st = CreateStack();
                await using var _ = st.Db;

                var confirmRes = await st.EnterpriseService.ConfirmDeliveryReceiptAsync(
                    custA.Id,
                    ctx6.OrderId,
                    new ConfirmOrderDeliveryReceiptRequest
                    {
                        DeliveryId = ctx6.DeliveryId,
                        Notes = "Da nhan du hang tot, dong y giai ngan."
                    });
                Assert.Equal(OrderStatus.Delivered, confirmRes.Status);

                var payoutReq = await st.EnterpriseService.CreatePayoutRequestAsync(
                    supA.Id,
                    ctx6.OrderId,
                    new CreateOrderPayoutRequest
                    {
                        BankCode = "ACB",
                        BankName = "Ngân hàng Á Châu ACB",
                        AccountNumber = "123456789012",
                        AccountHolderName = "XUONG BANH KIM LONG",
                        BranchName = "Chi nhanh Quan 1",
                        Notes = "Yeu cau giai ngan qua STK ACB truc tiep."
                    });

                Assert.Equal("Pending", payoutReq.Status);
                Assert.Contains("123456789012", payoutReq.BankSnapshotJson);
                Assert.Contains("XUONG BANH KIM LONG", payoutReq.BankSnapshotJson);

                var processedPayout = await st.EnterpriseService.ProcessPayoutRequestAsync(
                    admin.Id,
                    payoutReq.Id,
                    new ProcessOrderPayoutRequest
                    {
                        Approve = true,
                        PayoutReferenceCode = "TRF-PYT-ACB-006",
                        TransferProofImageUrl = SampleTransferProofUrl,
                        Notes = "Da chuyen khoan cho xuong va dinh kem PoD."
                    });

                Assert.Equal("PaidOutSimulated", processedPayout.Status);
                Assert.False(processedPayout.SandboxSimulated);
                Assert.Equal(SampleTransferProofUrl, processedPayout.TransferProofImageUrl);
                Assert.Equal("TRF-PYT-ACB-006", processedPayout.PayoutReferenceCode);

                var endFin6 = await st.EnterpriseService.GetFinancialSummaryAsync(supA.Id, false, true, ctx6.OrderId);
                Assert.Equal(0m, endFin6.EligibleAmount);
                Assert.Equal(ctx6.PaidAmount, endFin6.PaidOutSimulatedAmount);
                var finPayout = Assert.Single(endFin6.PayoutRequests);
                Assert.Equal(SampleTransferProofUrl, finPayout.TransferProofImageUrl);

                output.WriteLine($"[PASS] Scenario 6 Supplier Inline Bank Payout + Manual Proof: Payout={processedPayout.RequestNumber}");
            }
        }
        finally
        {
            await using var cleanupDb = CreateDbContext();
            var orphanIds = await cleanupDb.Orders
                .Where(o => o.CustomerId == custA.Id && o.RecipientName == "Cong ty Thuong Mai Anh Trang")
                .Select(o => o.Id)
                .ToArrayAsync();
            var orderIdArray = createdOrderIds.Concat(orphanIds).Distinct().ToArray();

            if (orderIdArray.Length > 0)
            {
                await cleanupDb.OrderFinancialLedgerEntries.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.OrderRefundCases.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.OrderDeliveryDisputes.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.SupplierPayoutRequests.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.Notifications.Where(x => x.ReferenceId.HasValue && orderIdArray.Contains(x.ReferenceId.Value)).ExecuteDeleteAsync();

                await cleanupDb.Payments.Where(x => x.OrderId.HasValue && orderIdArray.Contains(x.OrderId.Value)).ExecuteDeleteAsync();
                await cleanupDb.OrderPaymentParts.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.OrderPaymentObligations.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.InventoryReservations.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.OrderCancellationRequests.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.PackagingFeeChangeRequests.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();

                var deliveryIds = await cleanupDb.Deliveries
                    .Where(x => x.OrderId.HasValue && orderIdArray.Contains(x.OrderId.Value))
                    .Select(x => x.Id)
                    .ToArrayAsync();
                if (deliveryIds.Length > 0)
                    await cleanupDb.DeliveryProofs.Where(x => deliveryIds.Contains(x.DeliveryId)).ExecuteDeleteAsync();

                await cleanupDb.Deliveries.Where(x => x.OrderId.HasValue && orderIdArray.Contains(x.OrderId.Value)).ExecuteDeleteAsync();
                await cleanupDb.OrderItems.Where(x => orderIdArray.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.WorkflowEvents.Where(x => x.AggregateType == "Order" && orderIdArray.Contains(x.AggregateId)).ExecuteDeleteAsync();
                await cleanupDb.Orders.Where(x => orderIdArray.Contains(x.Id)).ExecuteDeleteAsync();

                await cleanupDb.ProductVariants
                    .Where(v => v.Id == productAndVariant.Variant.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, currentStockSnapshot));
            }
        }
    }

    [Fact]
    public async Task SupplierDashboardSummary_CombinesB2BDirectAndRfqContracts_ExcludingCancelledAndRefunds_PostgreSql()
    {
        NpgsqlConnection.ClearAllPools();

        await using var lookupDb = CreateDbContext();
        var custA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "cust.a@test.mooncake.local");
        var supA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "sup.a@test.mooncake.local");
        var supProfile = await lookupDb.SupplierProfiles.AsNoTracking()
            .FirstAsync(p => p.UserId == supA.Id && !p.IsDeleted);
        var shop = await lookupDb.Shops.AsNoTracking()
            .FirstAsync(s => !s.IsDeleted && s.Status == ShopStatus.Active && (s.OwnerUserId == supA.Id || s.SupplierId == supProfile.Id));

        var createdOrderIds = new List<Guid>();
        var createdContractIds = new List<Guid>();

        try
        {
            var stackBefore = CreateStack();
            await using var _beforeDb = stackBefore.Db;
            var baseline = await stackBefore.EnterpriseService.GetSupplierOrderDashboardSummaryAsync(supA.Id);

            var now = DateTime.UtcNow;
            var deliveredPartialRefundOrderId = Guid.NewGuid();
            var cancelledRefundedOrderId = Guid.NewGuid();
            var pendingUnpaidOrderId = Guid.NewGuid();
            var activeContractId = Guid.NewGuid();
            var cancelledContractId = Guid.NewGuid();

            createdOrderIds.AddRange([deliveredPartialRefundOrderId, cancelledRefundedOrderId, pendingUnpaidOrderId]);
            createdContractIds.AddRange([activeContractId, cancelledContractId]);

            await using (var seedDb = CreateDbContext())
            {
                seedDb.Orders.AddRange(
                    new Order
                    {
                        Id = deliveredPartialRefundOrderId,
                        OrderNumber = $"TST-REV-DLV-{Guid.NewGuid():N}"[..20],
                        CustomerId = custA.Id,
                        ShopId = shop.Id,
                        Status = OrderStatus.Delivered,
                        Subtotal = 10_000_000m,
                        TotalAmount = 10_000_000m,
                        RecipientName = "Test Rev Delivered",
                        RecipientPhone = "0901112223",
                        DeliveryAddress = "TP HCM",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new Order
                    {
                        Id = cancelledRefundedOrderId,
                        OrderNumber = $"TST-REV-CAN-{Guid.NewGuid():N}"[..20],
                        CustomerId = custA.Id,
                        ShopId = shop.Id,
                        Status = OrderStatus.Cancelled,
                        Subtotal = 6_050_000m,
                        TotalAmount = 6_050_000m,
                        RecipientName = "Test Rev Cancelled",
                        RecipientPhone = "0901112224",
                        DeliveryAddress = "TP HCM",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new Order
                    {
                        Id = pendingUnpaidOrderId,
                        OrderNumber = $"TST-REV-PEN-{Guid.NewGuid():N}"[..20],
                        CustomerId = custA.Id,
                        ShopId = shop.Id,
                        Status = OrderStatus.Pending,
                        Subtotal = 4_000_000m,
                        TotalAmount = 4_000_000m,
                        RecipientName = "Test Rev Pending",
                        RecipientPhone = "0901112225",
                        DeliveryAddress = "TP HCM",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    });

                seedDb.OrderFinancialLedgerEntries.AddRange(
                    new OrderFinancialLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = deliveredPartialRefundOrderId,
                        ShopId = shop.Id,
                        CustomerId = custA.Id,
                        EntryType = "Captured",
                        BalanceBucket = "Held",
                        Amount = 10_000_000m,
                        Description = "Test capture delivered order",
                        IdempotencyKey = $"TST-REV-CAP1-{Guid.NewGuid():N}",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new OrderFinancialLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = deliveredPartialRefundOrderId,
                        ShopId = shop.Id,
                        CustomerId = custA.Id,
                        EntryType = "RefundedSimulated",
                        BalanceBucket = "Refunded",
                        Amount = 2_000_000m,
                        Description = "Test partial refund delivered order",
                        IdempotencyKey = $"TST-REV-REF1-{Guid.NewGuid():N}",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new OrderFinancialLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = cancelledRefundedOrderId,
                        ShopId = shop.Id,
                        CustomerId = custA.Id,
                        EntryType = "Captured",
                        BalanceBucket = "Held",
                        Amount = 6_050_000m,
                        Description = "Test capture cancelled order",
                        IdempotencyKey = $"TST-REV-CAP2-{Guid.NewGuid():N}",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new OrderFinancialLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        OrderId = cancelledRefundedOrderId,
                        ShopId = shop.Id,
                        CustomerId = custA.Id,
                        EntryType = "RefundedSimulated",
                        BalanceBucket = "Refunded",
                        Amount = 6_050_000m,
                        Description = "Test full refund cancelled order",
                        IdempotencyKey = $"TST-REV-REF2-{Guid.NewGuid():N}",
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    });

                seedDb.Contracts.AddRange(
                    new Contract
                    {
                        Id = activeContractId,
                        CustomerId = custA.Id,
                        SupplierId = supProfile.Id,
                        ContractNumber = $"CT-REV-ACT-{Guid.NewGuid():N}"[..20],
                        Title = "Test Active RFQ Contract",
                        TotalValue = 15_000_000m,
                        DepositAmount = 4_500_000m,
                        Status = ContractStatus.Active,
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new Contract
                    {
                        Id = cancelledContractId,
                        CustomerId = custA.Id,
                        SupplierId = supProfile.Id,
                        ContractNumber = $"CT-REV-CAN-{Guid.NewGuid():N}"[..20],
                        Title = "Test Cancelled RFQ Contract",
                        TotalValue = 9_000_000m,
                        DepositAmount = 2_700_000m,
                        Status = ContractStatus.Cancelled,
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    });

                seedDb.ContractMilestones.AddRange(
                    new ContractMilestone
                    {
                        Id = Guid.NewGuid(),
                        ContractId = activeContractId,
                        SequenceNumber = 1,
                        Name = "Deposit 30%",
                        Amount = 4_500_000m,
                        IsDeposit = true,
                        Status = MilestoneStatus.Paid,
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    },
                    new ContractMilestone
                    {
                        Id = Guid.NewGuid(),
                        ContractId = activeContractId,
                        SequenceNumber = 2,
                        Name = "Balance 70%",
                        Amount = 10_500_000m,
                        IsDeposit = false,
                        Status = MilestoneStatus.InProgress,
                        CreatedAt = now,
                        CreatedBy = custA.Id
                    });

                await seedDb.SaveChangesAsync();
            }

            var stackAfter = CreateStack();
            await using var _afterDb = stackAfter.Db;
            var after = await stackAfter.EnterpriseService.GetSupplierOrderDashboardSummaryAsync(supA.Id);

            Assert.Equal(3, after.TotalOrders - baseline.TotalOrders);
            Assert.Equal(1, after.PendingOrders - baseline.PendingOrders);
            Assert.Equal(1, after.ActiveDirectOrdersCount - baseline.ActiveDirectOrdersCount);
            Assert.Equal(8_000_000m, after.DirectOrderRevenue - baseline.DirectOrderRevenue);
            Assert.Equal(8_000_000m, after.DirectOrderPaidRevenue - baseline.DirectOrderPaidRevenue);

            Assert.Equal(1, after.ActiveContractsCount - baseline.ActiveContractsCount);
            Assert.Equal(15_000_000m, after.ContractRevenue - baseline.ContractRevenue);
            Assert.Equal(4_500_000m, after.ContractPaidRevenue - baseline.ContractPaidRevenue);

            Assert.Equal(23_000_000m, after.TotalRevenue - baseline.TotalRevenue);
            Assert.Equal(8_050_000m, after.RefundedAmount - baseline.RefundedAmount);
            Assert.NotNull(after.DailyTrend);
            Assert.Equal(14, after.DailyTrend!.Count);
            Assert.Equal(23_000_000m, after.DailyTrend.Sum(p => p.DirectOrderRevenue + p.ContractRevenue) - (baseline.DailyTrend?.Sum(p => p.DirectOrderRevenue + p.ContractRevenue) ?? 0m));
            Assert.NotNull(after.MonthlyTrend);
            Assert.Equal(6, after.MonthlyTrend!.Count);

            output.WriteLine($"[PASS] Combined Supplier Revenue: Direct={after.DirectOrderRevenue - baseline.DirectOrderRevenue:N0}, RFQContract={after.ContractRevenue - baseline.ContractRevenue:N0}, Total={after.TotalRevenue - baseline.TotalRevenue:N0}");
        }
        finally
        {
            await using var cleanupDb = CreateDbContext();
            if (createdOrderIds.Count > 0)
            {
                await cleanupDb.OrderFinancialLedgerEntries.Where(x => createdOrderIds.Contains(x.OrderId)).ExecuteDeleteAsync();
                await cleanupDb.Orders.Where(x => createdOrderIds.Contains(x.Id)).ExecuteDeleteAsync();
            }
            if (createdContractIds.Count > 0)
            {
                await cleanupDb.ContractMilestones.Where(x => createdContractIds.Contains(x.ContractId)).ExecuteDeleteAsync();
                await cleanupDb.Contracts.Where(x => createdContractIds.Contains(x.Id)).ExecuteDeleteAsync();
            }
        }
    }
}
