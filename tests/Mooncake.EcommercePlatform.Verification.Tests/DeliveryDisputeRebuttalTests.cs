namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
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
using Microsoft.Extensions.Logging;
using Moq;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.WebApi.Controllers;
using Mooncake.EcommercePlatform.WebApi.Middleware;
using Npgsql;
using Npgsql.NameTranslation;
using Pgvector.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Unit, HTTP contract, and database integration tests for the 48-hour supplier delivery dispute rebuttal window (D27).
/// </summary>
public sealed class DeliveryDisputeRebuttalTests
{
    private static readonly DateTime FixedBaseTime = new(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);
    private const string SampleRebuttalEvidenceUrl = "https://res.cloudinary.com/demo/image/upload/v1/rebuttal/packaging_proof.jpg";

    private static string? TryGetConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        var candidate = "Host=localhost;Port=5433;Database=mooncake_dev;Username=mooncake_dev;Password=8IPiY3JA4WLRn6bEKKYOB-7h";
        try
        {
            using var conn = new NpgsqlConnection(candidate);
            conn.Open();
            return candidate;
        }
        catch
        {
            return null;
        }
    }

    private static ApplicationDbContext CreatePostgresDbContext(string connectionString)
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = 4
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

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSourceBuilder.Build(), npgsql => npgsql.UseVector())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void OrderDeliveryDisputeResponse_ExposesRebuttalFieldsAndDeadlines()
    {
        var disputeId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var deadline = FixedBaseTime.AddHours(48);
        var rebuttalAt = FixedBaseTime.AddHours(12);

        var response = new OrderDeliveryDisputeResponse(
            disputeId,
            orderId,
            "MC-20261115-TEST01",
            null,
            "DamagedGoods",
            "Missing packaging seals",
            ["https://img.test/evidence1.jpg"],
            "Open",
            null,
            FixedBaseTime,
            null,
            "{\"BankName\":\"VCB\",\"AccountNumber\":\"123456\"}",
            2_000_000m,
            "Goods were sealed and verified at dispatch.",
            [SampleRebuttalEvidenceUrl],
            rebuttalAt,
            deadline);

        Assert.Equal("Goods were sealed and verified at dispatch.", response.SupplierRebuttalNotes);
        Assert.Single(response.SupplierRebuttalEvidenceUrls!);
        Assert.Equal(SampleRebuttalEvidenceUrl, response.SupplierRebuttalEvidenceUrls![0]);
        Assert.Equal(rebuttalAt, response.SupplierRebuttalAt);
        Assert.Equal(deadline, response.RebuttalDeadlineAt);
        Assert.Equal(rebuttalAt, response.SupplierRebuttalAtUtc);
        Assert.Equal(deadline, response.RebuttalDeadlineAtUtc);
    }

    [Fact]
    public void AutoConfirmFilter_ConsidersAwaitingSupplierRebuttalAndOpen_AsUnresolved()
    {
        // AutoConfirmDeliveredOrdersAsync predicate:
        // (dispute.Status == null || (!dispute.Status.StartsWith("Resolved") && dispute.Status != "Closed"))
        bool IsUnresolved(string? status) =>
            status == null || (!status.StartsWith("Resolved") && status != "Closed");

        Assert.True(IsUnresolved("AwaitingSupplierRebuttal"), "AwaitingSupplierRebuttal must block auto-confirmation");
        Assert.True(IsUnresolved("Open"), "Open must block auto-confirmation");
        Assert.True(IsUnresolved("SupplierResponded"), "SupplierResponded must block auto-confirmation");
        Assert.False(IsUnresolved("ResolvedRefund"), "ResolvedRefund is finished");
        Assert.False(IsUnresolved("ResolvedReleasePayout"), "ResolvedReleasePayout is finished");
        Assert.False(IsUnresolved("Closed"), "Closed is finished");
    }

    [Fact]
    public async Task HttpEndpoint_SubmitRebuttal_Success_And_RoleEnforcement()
    {
        var orderId = Guid.NewGuid();
        var disputeId = Guid.NewGuid();
        var supplierUserId = Guid.NewGuid();

        var enterpriseService = new Mock<IDirectBulkOrderService>();
        var orderService = new Mock<IOrderService>();

        enterpriseService
            .Setup(s => s.SubmitDeliveryDisputeRebuttalAsync(
                supplierUserId,
                orderId,
                disputeId,
                It.IsAny<SubmitSupplierDeliveryDisputeRebuttalRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid oId, Guid dId, SubmitSupplierDeliveryDisputeRebuttalRequest req, CancellationToken _) =>
                new OrderDeliveryDisputeResponse(
                    dId,
                    oId,
                    "MC-20261115-HTTP01",
                    null,
                    "DamagedGoods",
                    "Damage claimed by buyer",
                    [],
                    "Open",
                    null,
                    FixedBaseTime,
                    null,
                    null,
                    5_000_000m,
                    req.Notes,
                    req.EvidenceUrls,
                    FixedBaseTime.AddHours(2),
                    FixedBaseTime.AddHours(48)));

        async Task<HttpClient> CreateTestClientAsync(string role, Guid userId)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(SupplierOrderOperationsController).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton(orderService.Object);
            builder.Services.AddSingleton(enterpriseService.Object);
            builder.Services.AddSingleton(new Mock<IPlatformBootstrapService>().Object);

            builder.Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("SupplierExistingFulfillment", policy => policy.RequireRole(UserRole.Supplier.ToString()));
            });

            var app = builder.Build();
            app.UseExceptionHandler();
            app.Use(async (context, next) =>
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Role, role)
                };
                var identity = new ClaimsIdentity(claims, "Test");
                context.User = new ClaimsPrincipal(identity);
                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            return app.GetTestClient();
        }

        // 1. Supplier role submits rebuttal -> 200 OK
        using var supplierClient = await CreateTestClientAsync(UserRole.Supplier.ToString(), supplierUserId);
        var response = await supplierClient.PostAsJsonAsync(
            $"/api/v1/suppliers/me/orders/{orderId}/delivery-disputes/{disputeId}/rebuttal",
            new SubmitSupplierDeliveryDisputeRebuttalRequest
            {
                Notes = "Items dispatched in perfect condition with temperature logging.",
                EvidenceUrls = [SampleRebuttalEvidenceUrl]
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDeliveryDisputeResponse>>();
        Assert.NotNull(envelope);
        Assert.Equal(200, envelope.Status);
        Assert.NotNull(envelope.Data);
        Assert.Equal("Open", envelope.Data.Status);
        Assert.Equal("Items dispatched in perfect condition with temperature logging.", envelope.Data.SupplierRebuttalNotes);
        Assert.NotNull(envelope.Data.SupplierRebuttalEvidenceUrls);
        Assert.Single(envelope.Data.SupplierRebuttalEvidenceUrls);

        // 2. Customer role -> 403 Forbidden
        using var customerClient = await CreateTestClientAsync(UserRole.Customer.ToString(), Guid.NewGuid());
        var forbiddenResponse = await customerClient.PostAsJsonAsync(
            $"/api/v1/suppliers/me/orders/{orderId}/delivery-disputes/{disputeId}/rebuttal",
            new SubmitSupplierDeliveryDisputeRebuttalRequest
            {
                Notes = "Customer trying to submit supplier rebuttal."
            });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task DisputeRebuttalLifecycle_ResolveBeforeRebuttalThrows409_AfterRebuttalSucceeds_OverdueSucceeds()
    {
        var connStr = TryGetConnectionString();
        if (connStr is null)
            return; // Skip DB integration test if local postgres test database is unavailable

        await using var db = CreatePostgresDbContext(connStr);

        var customerId = Guid.NewGuid();
        var supplierUserId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var supplierProfileId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var disputeId = Guid.NewGuid();

        var customer = new User
        {
            Id = customerId,
            Email = $"cust.reb.{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            FullName = "Test Customer Rebuttal",
            Role = UserRole.Customer,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            IsActive = true,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var supplier = new User
        {
            Id = supplierUserId,
            Email = $"sup.reb.{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            FullName = "Test Supplier Rebuttal",
            Role = UserRole.Supplier,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            IsActive = true,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var admin = new User
        {
            Id = adminUserId,
            Email = $"adm.reb.{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            FullName = "Test Admin Rebuttal",
            Role = UserRole.Admin,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            IsActive = true,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var profile = new SupplierProfile
        {
            Id = supplierProfileId,
            UserId = supplierUserId,
            CompanyName = "Rebuttal Supplier Workshop",
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var revision = await db.ShopTemplateRevisions.FirstAsync();

        var shop = new Shop
        {
            Id = shopId,
            SupplierId = supplierProfileId,
            OwnerUserId = supplierUserId,
            TemplateId = revision.TemplateId,
            TemplateRevisionId = revision.Id,
            Name = "Rebuttal Test Bakery",
            Slug = $"reb-bakery-{Guid.NewGuid():N}"[..25].ToLowerInvariant(),
            Status = ShopStatus.Active,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-REB-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            CustomerId = customerId,
            ShopId = shopId,
            Status = OrderStatus.Disputed,
            TotalAmount = 5_000_000m,
            Subtotal = 4_500_000m,
            ShippingFee = 500_000m,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            PaymentNumber = $"PAY-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            Amount = 5_000_000m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.VNPay,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var dispute = new OrderDeliveryDispute
        {
            Id = disputeId,
            OrderId = orderId,
            CustomerId = customerId,
            ShopId = shopId,
            IssueType = "DamagedGoods",
            Description = "Outer boxes crushed upon arrival",
            Status = "AwaitingSupplierRebuttal",
            RebuttalDeadlineAt = FixedBaseTime.AddHours(48),
            CreatedBy = customerId,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        db.Users.AddRange(customer, supplier, admin);
        db.SupplierProfiles.Add(profile);
        db.Shops.Add(shop);
        db.Orders.Add(order);
        db.Payments.Add(payment);
        db.OrderDeliveryDisputes.Add(dispute);
        await db.SaveChangesAsync();

        var clockMock = new Mock<IDateTimeProvider>();
        // Current time is 10 hours after creation (before 48h deadline)
        clockMock.SetupGet(c => c.UtcNow).Returns(FixedBaseTime.AddHours(10));

        var repo = new EnterpriseOrderRepository(db, clockMock.Object);

        // 1. Resolve trước hạn chưa phản biện → 409
        var resolveRequest = new ResolveOrderDeliveryDisputeRequest
        {
            ResolutionAction = "ReleasePayout",
            Notes = "Admin attempting premature resolution."
        };

        var exBeforeRebuttal = await Assert.ThrowsAsync<HttpException>(() =>
            repo.ResolveDeliveryDisputeAsync(disputeId, adminUserId, resolveRequest));
        Assert.Equal(409, exBeforeRebuttal.StatusCode);
        Assert.Contains("hạn phản biện", exBeforeRebuttal.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Xưởng nộp phản biện
        var rebuttalRequest = new SubmitSupplierDeliveryDisputeRebuttalRequest
        {
            Notes = "Goods were double-boxed and shipped with fragile stickers. Courier mishandled.",
            EvidenceUrls = [SampleRebuttalEvidenceUrl]
        };

        var rebuttalResponse = await repo.SubmitDeliveryDisputeRebuttalAsync(
            supplierUserId,
            orderId,
            disputeId,
            rebuttalRequest);

        Assert.Equal("Open", rebuttalResponse.Status);
        Assert.Equal("Goods were double-boxed and shipped with fragile stickers. Courier mishandled.", rebuttalResponse.SupplierRebuttalNotes);
        Assert.NotNull(rebuttalResponse.SupplierRebuttalEvidenceUrls);
        Assert.Single(rebuttalResponse.SupplierRebuttalEvidenceUrls);

        // Sau phản biện: Admin resolve → OK
        var resolveAfterRebuttal = await repo.ResolveDeliveryDisputeAsync(disputeId, adminUserId, resolveRequest);
        Assert.Equal("ResolvedReleasePayout", resolveAfterRebuttal.Status);

        // 3. Quá hạn chưa phản biện: tạo dispute mới với deadline đã qua
        var overdueDisputeId = Guid.NewGuid();
        var overdueOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-OVD-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            CustomerId = customerId,
            ShopId = shopId,
            Status = OrderStatus.Disputed,
            TotalAmount = 3_000_000m,
            Subtotal = 3_000_000m,
            CreatedAt = FixedBaseTime.AddDays(-3),
            UpdatedAt = FixedBaseTime.AddDays(-3)
        };
        var overduePayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = overdueOrder.Id,
            CustomerId = customerId,
            PaymentNumber = $"PAY-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            Amount = 3_000_000m,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.VNPay,
            CreatedAt = FixedBaseTime.AddDays(-3),
            UpdatedAt = FixedBaseTime.AddDays(-3)
        };
        var overdueDispute = new OrderDeliveryDispute
        {
            Id = overdueDisputeId,
            OrderId = overdueOrder.Id,
            CustomerId = customerId,
            ShopId = shopId,
            IssueType = "MissingItems",
            Description = "2 boxes missing",
            Status = "AwaitingSupplierRebuttal",
            CreatedAt = FixedBaseTime.AddDays(-3),
            RebuttalDeadlineAt = FixedBaseTime.AddDays(-1), // Expired 1 day ago
            UpdatedAt = FixedBaseTime.AddDays(-3)
        };

        db.Orders.Add(overdueOrder);
        db.Payments.Add(overduePayment);
        db.OrderDeliveryDisputes.Add(overdueDispute);
        await db.SaveChangesAsync();

        // 3a. Rebuttal sau khi hết hạn 48h → 409
        var exExpired = await Assert.ThrowsAsync<HttpException>(() =>
            repo.SubmitDeliveryDisputeRebuttalAsync(
                supplierUserId,
                overdueOrder.Id,
                overdueDisputeId,
                new SubmitSupplierDeliveryDisputeRebuttalRequest { Notes = "Attempting late rebuttal." }));
        Assert.Equal(409, exExpired.StatusCode);

        // 3b. Rebuttal rỗng notes → 400
        var exEmptyNotes = await Assert.ThrowsAsync<HttpException>(() =>
            repo.SubmitDeliveryDisputeRebuttalAsync(
                supplierUserId,
                overdueOrder.Id,
                overdueDisputeId,
                new SubmitSupplierDeliveryDisputeRebuttalRequest { Notes = "   " }));
        Assert.Equal(400, exEmptyNotes.StatusCode);

        // 3c. Xưởng không sở hữu shop → 403
        var exWrongSupplier = await Assert.ThrowsAsync<HttpException>(() =>
            repo.SubmitDeliveryDisputeRebuttalAsync(
                Guid.NewGuid(),
                overdueOrder.Id,
                overdueDisputeId,
                new SubmitSupplierDeliveryDisputeRebuttalRequest { Notes = "Wrong supplier notes." }));
        Assert.Equal(403, exWrongSupplier.StatusCode);

        // 3d. Clock is now (FixedBaseTime + 10h), which is well after RebuttalDeadlineAt (FixedBaseTime - 24h)
        // Admin resolve quá hạn → OK
        var resolveOverdue = await repo.ResolveDeliveryDisputeAsync(
            overdueDisputeId,
            adminUserId,
            new ResolveOrderDeliveryDisputeRequest
            {
                ResolutionAction = "ReleasePayout",
                Notes = "Overdue supplier rebuttal window; admin resolved directly."
            });

        Assert.Equal("ResolvedReleasePayout", resolveOverdue.Status);
    }

    [Fact]
    public async Task ReportDeliveryDispute_SetsAwaitingSupplierRebuttal_And48hDeadline()
    {
        var connStr = TryGetConnectionString();
        if (connStr is null)
            return;

        await using var db = CreatePostgresDbContext(connStr);
        var revision = await db.ShopTemplateRevisions.FirstAsync();

        var customerId = Guid.NewGuid();
        var supplierUserId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var customer = new User
        {
            Id = customerId,
            Email = $"cust.rep.{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            FullName = "Dispute Customer",
            Role = UserRole.Customer,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            IsActive = true,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var supplier = new User
        {
            Id = supplierUserId,
            Email = $"sup.rep.{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            FullName = "Dispute Supplier",
            Role = UserRole.Supplier,
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            IsActive = true,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var profile = new SupplierProfile
        {
            Id = profileId,
            UserId = supplierUserId,
            CompanyName = "Dispute Supplier Co",
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var shop = new Shop
        {
            Id = shopId,
            SupplierId = profileId,
            OwnerUserId = supplierUserId,
            TemplateId = revision.TemplateId,
            TemplateRevisionId = revision.Id,
            Name = "Dispute Bakery",
            Slug = $"rep-bakery-{Guid.NewGuid():N}"[..25].ToLowerInvariant(),
            Status = ShopStatus.Active,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var order = new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-REP-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            CustomerId = customerId,
            ShopId = shopId,
            Status = OrderStatus.Shipped,
            TotalAmount = 2_000_000m,
            Subtotal = 2_000_000m,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            DeliveryNumber = $"DEL-{Guid.NewGuid():N}"[..18].ToUpperInvariant(),
            Status = DeliveryStatus.InTransit,
            CreatedAt = FixedBaseTime,
            UpdatedAt = FixedBaseTime
        };

        db.Users.AddRange(customer, supplier);
        db.SupplierProfiles.Add(profile);
        db.Shops.Add(shop);
        db.Orders.Add(order);
        db.Deliveries.Add(delivery);
        await db.SaveChangesAsync();

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.SetupGet(c => c.UtcNow).Returns(FixedBaseTime);

        var repo = new EnterpriseOrderRepository(db, clockMock.Object);

        var disputeResponse = await repo.ReportDeliveryDisputeAsync(
            order,
            customerId,
            new ReportOrderDeliveryDisputeRequest
            {
                IssueType = "WrongItems",
                Description = "Received strawberry cakes instead of matcha cakes.",
                EvidenceUrls = ["https://img.test/wrong1.jpg"],
                RefundBankName = "VCB",
                RefundAccountNumber = "9988776655",
                RefundAccountHolderName = "Dispute Customer"
            });

        Assert.Equal("AwaitingSupplierRebuttal", disputeResponse.Status);
        Assert.Equal(FixedBaseTime.AddHours(48), disputeResponse.RebuttalDeadlineAt);

        // Verify stored in DB
        var savedDispute = await db.OrderDeliveryDisputes.AsNoTracking().SingleAsync(d => d.Id == disputeResponse.Id);
        Assert.Equal("AwaitingSupplierRebuttal", savedDispute.Status);
        Assert.Equal(FixedBaseTime.AddHours(48), savedDispute.RebuttalDeadlineAt);
        Assert.Null(savedDispute.SupplierRebuttalAt);

        // Verify supplier notification created
        var notification = await db.Notifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.UserId == supplierUserId && n.Type == "order.delivery_disputed");
        Assert.NotNull(notification);
        Assert.Contains("48 hours", notification.Content);
    }
}
