namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.DTOs.Deliveries.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Packaging.Requests;
using Mooncake.EcommercePlatform.Application.Services.Implementations;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Options;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;
using Mooncake.EcommercePlatform.Infrastructure.Repositories;
using Mooncake.EcommercePlatform.Infrastructure.Services;
using Npgsql;
using Npgsql.NameTranslation;
using Pgvector.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

/// <summary>Real PostgreSQL concurrency, locking, multi-part VNPay, late-IPN, and simulated ledger integration tests for Phase 2.</summary>
public sealed class DirectBulkOrderP2DbIntegrationTests(ITestOutputHelper output)
{
    private const string SampleMockupUrl = "https://images.unsplash.com/photo-1631206753348-db44968fd440?auto=format&fit=crop&w=800&q=80";

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
            ?? configuration["DB_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

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

    private static ApplicationDbContext CreateSharedDbContext() => new ApplicationDbContext(SharedOptions.Value);

    private static (
        ApplicationDbContext Db,
        OrderService OrderService,
        DirectBulkOrderService EnterpriseService,
        CustomPackagingService PackagingService,
        DeliveryService DeliveryService,
        EnterpriseOrderRepository EnterpriseRepo,
        PaymentRepository PaymentRepo) CreateStack()
    {
        var db = CreateSharedDbContext();
        var clock = new DateTimeProvider();
        var orderRepo = new OrderRepository(db, clock);
        var enterpriseRepo = new EnterpriseOrderRepository(db, clock);
        var packagingRepo = new CustomPackagingRepository(db);
        var paymentRepo = new PaymentRepository(db, clock);
        var deliveryRepo = new DeliveryRepository(db);
        var vnPayOptions = Options.Create(new VnPayOptions
        {
            TmnCode = "MOONCAKE",
            HashSecret = "MOONCAKE_SANDBOX_SECRET_KEY_1234567890",
            BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        });
        var vnPayGateway = new VnPayGateway(vnPayOptions);

        var orderService = new OrderService(orderRepo, clock, enterpriseRepo);
        var enterpriseService = new DirectBulkOrderService(orderRepo, enterpriseRepo, vnPayGateway, clock);
        var packagingService = new CustomPackagingService(packagingRepo, clock);
        var deliveryService = new DeliveryService(deliveryRepo, clock);

        return (db, orderService, enterpriseService, packagingService, deliveryService, enterpriseRepo, paymentRepo);
    }

    [Fact]
    public async Task ExecutePhase2PostgreSqlConcurrencyAndFinanceSuiteAsync()
    {
        NpgsqlConnection.ClearAllPools();

        // Ensure seed data & policies exist first
        var seeder = new DirectBulkOrderTestSeedTests(output);
        await seeder.SeedDirectBulkOrderTestEnvironmentAsync();

        var evidenceReport = new Dictionary<string, object>();

        await using var lookupDb = CreateSharedDbContext();
        var custA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "cust.a@test.mooncake.local");
        var custB = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "cust.b@test.mooncake.local");
        var supA = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "sup.a@test.mooncake.local");
        var admin = await lookupDb.Users.AsNoTracking().SingleAsync(u => u.Email == "admin@test.mooncake.local");
        var shopA = await lookupDb.Shops.AsNoTracking().SingleAsync(s => s.Slug == "test-nguyet-pho-shop-a");
        var prodA1 = await lookupDb.Products.AsNoTracking().SingleAsync(p => p.ShopId == shopA.Id && p.SupportsCustomPackaging);
        var varV1 = await lookupDb.ProductVariants.AsNoTracking().SingleAsync(v => v.ProductId == prodA1.Id && v.NormalizedSku == "TEST-SA-P1-V1");
        var varV2 = await lookupDb.ProductVariants.AsNoTracking().SingleAsync(v => v.ProductId == prodA1.Id && v.NormalizedSku == "TEST-SA-P1-V2");
        var varRace = await lookupDb.ProductVariants.AsNoTracking().SingleAsync(v => v.ProductId == prodA1.Id && v.NormalizedSku == "TEST-SA-P1-RACE");

        // =========================================================================
        // 1. CONCURRENT STOCK RESERVATION RACE: stock = 10, custA(7) + custB(7)
        // =========================================================================
        await lookupDb.ProductVariants.Where(v => v.Id == varRace.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, 10));

        var deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        async Task<(string Actor, bool Succeeded, int StatusCode, string? ErrorCode, Guid? OrderId)> AttemptRaceOrderAsync(Guid customerId, string actorLabel)
        {
            var stack = CreateStack();
            await using var _ = stack.Db;
            try
            {
                var quote = await stack.OrderService.PreviewQuoteAsync(new OrderQuotePreviewRequest
                {
                    ShopId = shopA.Id,
                    ShippingZoneCode = "HCM_INNER",
                    Items = [new CreateOrderItemRequest { ProductId = prodA1.Id, VariantId = varRace.Id, Quantity = 7 }]
                });
                var created = await stack.OrderService.CreateAsync(customerId, Guid.NewGuid().ToString("N"), new CreateOrderRequest
                {
                    ShopId = shopA.Id,
                    DeliveryAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                    RecipientName = "TEST Procurement Lead",
                    RecipientPhone = "0988000101",
                    ShippingZoneCode = "HCM_INNER",
                    DeliveryDateExpected = deliveryDate,
                    AcceptedPriceVersion = quote.PriceVersion,
                    AcceptedTerms = true,
                    Items = [new CreateOrderItemRequest { ProductId = prodA1.Id, VariantId = varRace.Id, Quantity = 7 }]
                });
                return (actorLabel, true, 201, null, created.Id);
            }
            catch (HttpException ex)
            {
                return (actorLabel, false, ex.StatusCode, ex.ErrorCode, null);
            }
        }

        var raceOutcomes = await Task.WhenAll(
            AttemptRaceOrderAsync(custA.Id, "custA"),
            AttemptRaceOrderAsync(custB.Id, "custB"));

        var winners = raceOutcomes.Where(r => r.Succeeded).ToList();
        var losers = raceOutcomes.Where(r => !r.Succeeded).ToList();
        Assert.Single(winners);
        Assert.Single(losers);
        Assert.Equal(409, losers[0].StatusCode);
        Assert.Equal(OrderErrorCodes.InsufficientStock, losers[0].ErrorCode);

        var remainingRaceStock = await lookupDb.ProductVariants.AsNoTracking()
            .Where(v => v.Id == varRace.Id)
            .Select(v => v.StockQty)
            .SingleAsync();
        Assert.Equal(3, remainingRaceStock);

        evidenceReport["test1_stockConcurrencyRace"] = new
        {
            initialStock = 10,
            requestedByCustA = 7,
            requestedByCustB = 7,
            winner = winners[0],
            loser = losers[0],
            finalStockInPostgres = remainingRaceStock
        };
        output.WriteLine($"[PASS] Test 1 Stock Race: winner={winners[0].Actor}, loser={losers[0].Actor} ({losers[0].ErrorCode}), finalStock={remainingRaceStock}");

        // =========================================================================
        // 2. CONCURRENT STATE TRANSITION RACE: Confirm vs Cancel vs Expire Job
        // =========================================================================
        Guid stateRaceOrderId;
        int stockBeforeStateRace;
        {
            var setupStack = CreateStack();
            await using var _ = setupStack.Db;
            stockBeforeStateRace = await setupStack.Db.ProductVariants.AsNoTracking()
                .Where(v => v.Id == varV1.Id).Select(v => v.StockQty).SingleAsync();
            var created = await setupStack.OrderService.CreateAsync(custA.Id, Guid.NewGuid().ToString("N"), new CreateOrderRequest
            {
                ShopId = shopA.Id,
                DeliveryAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                RecipientName = "TEST Procurement Lead",
                RecipientPhone = "0988000101",
                ShippingZoneCode = "SELF_PICKUP",
                DeliveryDateExpected = deliveryDate,
                Items = [new CreateOrderItemRequest { ProductId = prodA1.Id, VariantId = varV1.Id, Quantity = 10 }]
            });
            stateRaceOrderId = created.Id;

            // Force response/reservation deadline into the past so ExpireOverdueOrdersAsync competes directly with Confirm and Cancel
            var overdue = DateTime.UtcNow.AddMinutes(-5);
            await setupStack.Db.Orders.Where(o => o.Id == stateRaceOrderId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.ResponseDeadlineAt, overdue)
                    .SetProperty(o => o.ReservationExpiresAt, overdue));
        }

        var confirmTask = Task.Run(async () =>
        {
            var s = CreateStack();
            await using var _ = s.Db;
            try
            {
                await s.OrderService.ConfirmAsync(supA.Id, stateRaceOrderId);
                return "ConfirmSucceeded";
            }
            catch (HttpException ex)
            {
                return $"ConfirmRejected:{ex.StatusCode}:{ex.ErrorCode}";
            }
        });

        var cancelTask = Task.Run(async () =>
        {
            var s = CreateStack();
            await using var _ = s.Db;
            try
            {
                await s.OrderService.CancelAsync(custA.Id, stateRaceOrderId, new CancelOrderRequest { Reason = "Concurrent customer cancel test" });
                return "CancelSucceeded";
            }
            catch (HttpException ex)
            {
                return $"CancelRejected:{ex.StatusCode}:{ex.ErrorCode}";
            }
        });

        var expireTask = Task.Run(async () =>
        {
            var s = CreateStack();
            await using var _ = s.Db;
            var count = await s.EnterpriseRepo.ExpireOverdueOrdersAsync();
            return $"ExpireProcessed:{count}";
        });

        var stateRaceResults = await Task.WhenAll(confirmTask, cancelTask, expireTask);

        // Run ExpireOverdueOrdersAsync a second time to prove idempotency (no double stock release)
        {
            var s2 = CreateStack();
            await using var _ = s2.Db;
            await s2.EnterpriseRepo.ExpireOverdueOrdersAsync();

            var finalRaceOrder = await s2.Db.Orders.AsNoTracking().SingleAsync(o => o.Id == stateRaceOrderId);
            var releaseMovements = await s2.Db.ProductInventoryMovements.AsNoTracking()
                .Where(m => m.SourceId == stateRaceOrderId.ToString() && m.SourceType == "OrderCancellation")
                .ToListAsync();
            var stockAfterStateRace = await s2.Db.ProductVariants.AsNoTracking()
                .Where(v => v.Id == varV1.Id).Select(v => v.StockQty).SingleAsync();

            if (finalRaceOrder.Status == OrderStatus.Confirmed)
            {
                Assert.Empty(releaseMovements);
                Assert.Equal(stockBeforeStateRace - 10, stockAfterStateRace);
            }
            else
            {
                Assert.True(finalRaceOrder.Status is OrderStatus.Cancelled or OrderStatus.Expired);
                Assert.Single(releaseMovements);
                Assert.Equal(stockBeforeStateRace, stockAfterStateRace);
            }

            evidenceReport["test2_stateTransitionRace"] = new
            {
                orderId = stateRaceOrderId,
                taskOutcomes = stateRaceResults,
                finalStatus = finalRaceOrder.Status.ToString(),
                releaseMovementCount = releaseMovements.Count,
                stockBefore = stockBeforeStateRace,
                stockAfter = stockAfterStateRace
            };
            output.WriteLine($"[PASS] Test 2 State Race: outcomes=[{string.Join(", ", stateRaceResults)}], final={finalRaceOrder.Status}, releaseMovements={releaseMovements.Count}");
        }

        // =========================================================================
        // 3. 250M+ MULTI-LINE + CUSTOM PACKAGING + SUPPLEMENTAL FEE + 100M SPLIT + DUPLICATE IPN + POD + PAYOUT
        // =========================================================================
        {
            var s = CreateStack();
            await using var _ = s.Db;

            var multiLineItems = new List<CreateOrderItemRequest>
            {
                new() { ProductId = prodA1.Id, VariantId = varV1.Id, Quantity = 150 },
                new() { ProductId = prodA1.Id, VariantId = varV2.Id, Quantity = 300 }
            };

            var quote = await s.OrderService.PreviewQuoteAsync(new OrderQuotePreviewRequest
            {
                ShopId = shopA.Id,
                ShippingZoneCode = "HCM_INNER",
                Items = multiLineItems
            });

            Assert.True(quote.PayableTotal > 250_000_000m);
            Assert.Equal(0m, quote.ShippingFee); // Free shipping threshold >= 20M triggered!
            Assert.True(quote.DepositRequired);
            Assert.Equal(30m, quote.DepositPercent);

            // Stale price version must be rejected with 409 PRICE_CHANGED + fresh quote in ErrorData
            var staleEx = await Assert.ThrowsAsync<HttpException>(() =>
                s.OrderService.CreateAsync(custA.Id, Guid.NewGuid().ToString("N"), new CreateOrderRequest
                {
                    ShopId = shopA.Id,
                    DeliveryAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                    RecipientName = "TEST Procurement Lead",
                    RecipientPhone = "0988000101",
                    ShippingZoneCode = "HCM_INNER",
                    DeliveryDateExpected = deliveryDate,
                    AcceptedPriceVersion = "PV1-STALE000000000000",
                    AcceptedTerms = true,
                    Items = multiLineItems
                }));
            Assert.Equal(409, staleEx.StatusCode);
            Assert.Equal(OrderErrorCodes.PriceChanged, staleEx.ErrorCode);
            Assert.NotNull(staleEx.ErrorData);

            // Valid order creation with atomic CustomPackaging
            var order = await s.OrderService.CreateAsync(custA.Id, Guid.NewGuid().ToString("N"), new CreateOrderRequest
            {
                ShopId = shopA.Id,
                DeliveryAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                RecipientName = "TEST Procurement Lead",
                RecipientPhone = "0988000101",
                ShippingZoneCode = "HCM_INNER",
                DeliveryDateExpected = deliveryDate,
                AcceptedPriceVersion = quote.PriceVersion,
                AcceptedTerms = true,
                Items = multiLineItems,
                CustomPackaging = new CreateCustomPackagingRequest
                {
                    CompanyName = "TEST Alpha Enterprise Co., Ltd.",
                    CompanyLogoUrl = SampleMockupUrl,
                    PackagingType = "TEST Royal Lacquer Gift Box",
                    Notes = "Gold foil logo on crimson lacquer lid"
                }
            });
            Assert.Equal(2, order.Items.Count);
            Assert.NotNull(order.CustomPackaging);

            // Supplier proposes alternate delivery date -> Customer approves -> Order Confirmed + Schedule generated
            var proposedDate = deliveryDate.AddDays(3);
            var afterProposal = await s.EnterpriseService.ProposeDeliveryDateAsync(supA.Id, order.Id, new ProposeOrderDeliveryDateRequest
            {
                ProposedDeliveryDate = proposedDate,
                Reason = "Need 3 extra days for hot-foil brass die fabrication."
            });
            Assert.Equal(OrderStatus.AwaitingCustomerDeliveryApproval, afterProposal.Status);

            var confirmed = await s.EnterpriseService.RespondDeliveryProposalAsync(custA.Id, order.Id, new RespondOrderDeliveryProposalRequest
            {
                Approve = true,
                Reason = "Approved 3-day extension."
            });
            Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
            Assert.Equal(proposedDate, confirmed.ConfirmedDeliveryDate);
            Assert.NotNull(confirmed.PaymentSummary);
            Assert.Equal(2, confirmed.PaymentSummary!.Obligations.Count);

            var depositOb = confirmed.PaymentSummary.Obligations.Single(o => o.ObligationType == "Deposit");
            var balanceOb = confirmed.PaymentSummary.Obligations.Single(o => o.ObligationType == "RemainingBalance");
            Assert.All(depositOb.Parts, p => Assert.True(p.Amount <= 100_000_000m));
            Assert.All(balanceOb.Parts, p => Assert.True(p.Amount <= 100_000_000m));
            Assert.True(balanceOb.Parts.Count >= 2);

            // StartProcessing blocked before deposit/packaging
            var beforeDepositEx = await Assert.ThrowsAsync<HttpException>(() => s.OrderService.StartProcessingAsync(supA.Id, order.Id));
            Assert.Equal(OrderErrorCodes.PackagingApprovalRequired, beforeDepositEx.ErrorCode);

            // Pay Deposit Part 1 + test concurrent DUPLICATE IPN
            var depPart = depositOb.Parts[0];
            var depUrl = await s.EnterpriseService.CreatePartVnPayUrlAsync(custA.Id, order.Id, depPart.Id, new CreateOrderPartVnPayUrlRequest
            {
                ReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua"
            }, "127.0.0.1");

            var dupIpnTasks = await Task.WhenAll(
                Task.Run(async () =>
                {
                    var sIpn1 = CreateStack();
                    await using var _1 = sIpn1.Db;
                    return await sIpn1.PaymentRepo.ConfirmVnPayPaymentAsync(depUrl.PaymentNumber);
                }),
                Task.Run(async () =>
                {
                    var sIpn2 = CreateStack();
                    await using var _2 = sIpn2.Db;
                    return await sIpn2.PaymentRepo.ConfirmVnPayPaymentAsync(depUrl.PaymentNumber);
                }));
            Assert.All(dupIpnTasks, p => Assert.NotNull(p));

            var capturedCountAfterDupIpn = await s.Db.OrderFinancialLedgerEntries.AsNoTracking()
                .CountAsync(l => l.OrderId == order.Id && l.EntryType == "Captured");
            Assert.Equal(1, capturedCountAfterDupIpn);

            // Supplier submits mockup revision with DesignFee = 6,000,000 -> creates PackagingFeeChangeRequest
            var pkgDetail = await s.PackagingService.SubmitRevisionAsync(supA.Id, order.Id, new SubmitPackagingRevisionRequest
            {
                MockupUrl = SampleMockupUrl,
                DesignFee = 6_000_000m,
                SupplierNotes = "Custom brass plate & crimson lacquer proof v1"
            });
            var revId = pkgDetail.Revisions[0].Id;

            // Customer approves mockup -> creates supplemental PackagingFeeAdjustment obligation without mutating settled Deposit
            await s.PackagingService.ReviewRevisionAsync(custA.Id, order.Id, revId, new ReviewPackagingRevisionRequest
            {
                Action = "Approve",
                Notes = "Approved brass plate design and +6M fee."
            });

            // StartProcessing MUST still be blocked until supplemental 6M obligation is paid!
            var beforeSuppPaidEx = await Assert.ThrowsAsync<HttpException>(() => s.OrderService.StartProcessingAsync(supA.Id, order.Id));
            Assert.Equal(OrderErrorCodes.PaymentRequired, beforeSuppPaidEx.ErrorCode);

            var summaryAfterFeeChange = await s.EnterpriseService.GetPaymentSummaryAsync(custA.Id, false, false, order.Id);
            Assert.Equal(3, summaryAfterFeeChange.Obligations.Count);
            var suppOb = summaryAfterFeeChange.Obligations.Single(o => o.ObligationType == "PackagingFeeAdjustment");
            Assert.Equal(6_000_000m, suppOb.TotalAmount);

            // Pay supplemental obligation + all RemainingBalance parts
            foreach (var openPart in summaryAfterFeeChange.Obligations.SelectMany(o => o.Parts).Where(p => p.Status != "Paid"))
            {
                var partUrl = await s.EnterpriseService.CreatePartVnPayUrlAsync(custA.Id, order.Id, openPart.Id, new CreateOrderPartVnPayUrlRequest
                {
                    ReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua"
                }, "127.0.0.1");
                var sPay = CreateStack();
                await using var _p = sPay.Db;
                await sPay.PaymentRepo.ConfirmVnPayPaymentAsync(partUrl.PaymentNumber);
            }

            // StartProcessing & MarkShipped now succeed!
            var processingOrder = await s.OrderService.StartProcessingAsync(supA.Id, order.Id);
            Assert.Equal(OrderStatus.Processing, processingOrder.Status);

            var shippedOrder = await s.OrderService.MarkShippedAsync(supA.Id, order.Id);
            Assert.Equal(OrderStatus.Shipped, shippedOrder.Status);

            // ConfirmDeliveryReceipt MUST fail before PoD photo!
            var noPodEx = await Assert.ThrowsAsync<HttpException>(() =>
                s.EnterpriseService.ConfirmDeliveryReceiptAsync(custA.Id, order.Id, new ConfirmOrderDeliveryReceiptRequest()));
            Assert.Equal(OrderErrorCodes.DeliveryProofRequired, noPodEx.ErrorCode);

            // Supplier creates 1 shipment & uploads PoD photo -> AwaitingCustomerConfirmation
            var shipment = await s.DeliveryService.CreateAsync(supA.Id, new CreateDeliveryRequest
            {
                OrderId = order.Id,
                DeliveryType = DeliveryType.SelfArranged,
                ScheduledDate = proposedDate,
                FromAddress = "12 Artisan Street, District 7, Ho Chi Minh City",
                ToAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                RecipientName = "TEST Alpha Procurement",
                RecipientPhone = "0988000101",
                TrackingCode = "TRK-ALPHA-250M"
            });
            await s.DeliveryService.SubmitProofAsync(supA.Id, shipment.Id, new CreateDeliveryProofRequest
            {
                ProofType = ProofType.Delivery,
                PhotoUrl = SampleMockupUrl,
                Notes = "450 boxes delivered at lobby dock"
            });

            var shipmentDetail = await s.DeliveryService.GetAsync(custA.Id, shipment.Id);
            Assert.Equal(DeliveryStatus.AwaitingCustomerConfirmation, shipmentDetail.Status);

            // Customer confirms receipt -> Order Delivered & Held unlocked to Eligible!
            var deliveredOrder = await s.EnterpriseService.ConfirmDeliveryReceiptAsync(custA.Id, order.Id, new ConfirmOrderDeliveryReceiptRequest
            {
                DeliveryId = shipment.Id,
                Notes = "Inspected all 450 boxes in pristine condition."
            });
            Assert.Equal(OrderStatus.Delivered, deliveredOrder.Status);
            Assert.Equal(0m, deliveredOrder.FinancialSummary!.HeldAmount);
            Assert.Equal(quote.PayableTotal + 6_000_000m, deliveredOrder.FinancialSummary.EligibleAmount);

            // Supplier requests simulated payout -> Admin approves simulated payout
            var payoutReq = await s.EnterpriseService.CreatePayoutRequestAsync(supA.Id, order.Id, new CreateOrderPayoutRequest
            {
                Notes = "Requesting sandbox payout after PoD confirmation."
            });
            Assert.Equal("Pending", payoutReq.Status);
            Assert.True(payoutReq.SandboxSimulated);

            var processedPayout = await s.EnterpriseService.ProcessPayoutRequestAsync(admin.Id, payoutReq.Id, new ProcessOrderPayoutRequest
            {
                Approve = true,
                PayoutReferenceCode = "SIM-PAYOUT-ALPHA-001",
                Notes = "Sandbox payout approved and recorded."
            });
            Assert.Equal("PaidOutSimulated", processedPayout.Status);

            var finalFin = await s.EnterpriseService.GetFinancialSummaryAsync(supA.Id, true, false, order.Id);
            Assert.Equal(quote.PayableTotal + 6_000_000m, finalFin.PaidOutSimulatedAmount);
            Assert.Equal(0m, finalFin.EligibleAmount);

            evidenceReport["test3_enterprise250mMultiPartPackagingAndPayout"] = new
            {
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                initialQuoteTotal = quote.PayableTotal,
                finalOrderTotalWithPackagingDelta = finalFin.CapturedAmount,
                obligationsCount = summaryAfterFeeChange.Obligations.Count,
                partAmounts = summaryAfterFeeChange.Obligations.SelectMany(o => o.Parts).Select(p => p.Amount).ToArray(),
                duplicateIpnCapturedEntries = capturedCountAfterDupIpn,
                payoutRequestNumber = processedPayout.RequestNumber,
                paidOutSimulatedAmount = finalFin.PaidOutSimulatedAmount
            };
            output.WriteLine($"[PASS] Test 3 250M+ Enterprise Flow: Order={order.OrderNumber}, Total={finalFin.CapturedAmount:N0} VND, PaidOutSimulated={finalFin.PaidOutSimulatedAmount:N0} VND");
        }

        // =========================================================================
        // 4. LATE VNPAY IPN AFTER CANCELLATION/EXPIRY -> AUTO REFUND CASE (NO REVIVAL)
        // =========================================================================
        {
            var s = CreateStack();
            await using var _ = s.Db;

            var lateOrder = await s.OrderService.CreateAsync(custA.Id, Guid.NewGuid().ToString("N"), new CreateOrderRequest
            {
                ShopId = shopA.Id,
                DeliveryAddress = "101 Le Loi, District 1, Ho Chi Minh City",
                RecipientName = "TEST Procurement Lead",
                RecipientPhone = "0988000101",
                ShippingZoneCode = "HCM_INNER",
                DeliveryDateExpected = deliveryDate,
                Items = [new CreateOrderItemRequest { ProductId = prodA1.Id, VariantId = varV1.Id, Quantity = 20 }]
            });
            var confirmedLate = await s.OrderService.ConfirmAsync(supA.Id, lateOrder.Id);
            var firstPartId = confirmedLate.PaymentSummary!.Obligations[0].Parts[0].Id;
            var vnpUrl = await s.EnterpriseService.CreatePartVnPayUrlAsync(custA.Id, lateOrder.Id, firstPartId, new CreateOrderPartVnPayUrlRequest
            {
                ReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua"
            }, "127.0.0.1");

            // Expire the order before VNPay IPN arrives!
            var pastDeadline = DateTime.UtcNow.AddMinutes(-10);
            await s.Db.Orders.Where(o => o.Id == lateOrder.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(o => o.ReservationExpiresAt, pastDeadline));
            await s.EnterpriseRepo.ExpireOverdueOrdersAsync();

            var expiredCheck = await s.Db.Orders.AsNoTracking().SingleAsync(o => o.Id == lateOrder.Id);
            Assert.Equal(OrderStatus.Expired, expiredCheck.Status);

            // Now deliver LATE VNPay IPN!
            var sLateIpn = CreateStack();
            await using var _late = sLateIpn.Db;
            var latePayment = await sLateIpn.PaymentRepo.ConfirmVnPayPaymentAsync(vnpUrl.PaymentNumber);
            Assert.NotNull(latePayment);

            var afterLateIpnOrder = await sLateIpn.Db.Orders.AsNoTracking().SingleAsync(o => o.Id == lateOrder.Id);
            Assert.Equal(OrderStatus.Expired, afterLateIpnOrder.Status); // NEVER revived!

            var finSummary = await sLateIpn.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, lateOrder.Id);
            Assert.Single(finSummary.RefundCases);
            Assert.Equal("Pending", finSummary.RefundCases[0].Status);
            Assert.Equal(latePayment!.Amount, finSummary.RefundCases[0].RequestedAmount);

            // Admin processes the automatic refund case
            var processedRefund = await sLateIpn.EnterpriseService.ProcessRefundCaseAsync(admin.Id, finSummary.RefundCases[0].Id, new ProcessOrderRefundCaseRequest
            {
                Approve = true,
                ApprovedAmount = latePayment.Amount,
                RefundReferenceCode = "SIM-REFUND-LATEIPN-001",
                Notes = "Approved full refund for late VNPay IPN on expired order."
            });
            Assert.Equal("CompletedSimulated", processedRefund.Status);

            var finAfterRefund = await sLateIpn.EnterpriseService.GetFinancialSummaryAsync(custA.Id, false, false, lateOrder.Id);
            Assert.Equal(latePayment.Amount, finAfterRefund.RefundedSimulatedAmount);
            Assert.Equal(0m, finAfterRefund.HeldAmount);

            evidenceReport["test4_lateIpnAfterExpiryAutoRefund"] = new
            {
                orderId = lateOrder.Id,
                orderNumber = lateOrder.OrderNumber,
                statusAfterLateIpn = afterLateIpnOrder.Status.ToString(),
                latePaymentNumber = latePayment.PaymentNumber,
                autoRefundCaseNumber = processedRefund.CaseNumber,
                refundedSimulatedAmount = finAfterRefund.RefundedSimulatedAmount
            };
            output.WriteLine($"[PASS] Test 4 Late IPN After Expiry: Order={lateOrder.OrderNumber} stayed {afterLateIpnOrder.Status}, RefundCase={processedRefund.CaseNumber} refunded {finAfterRefund.RefundedSimulatedAmount:N0} VND");
        }

        // Reset race variant stock back to 10 so downstream E2E tests start clean
        await lookupDb.ProductVariants.Where(v => v.Id == varRace.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQty, 10));

        var outDir = @"d:\PRN232\mooncake-fe\.agents\evidence\P2\api";
        Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(
            Path.Combine(outDir, "p2_concurrency_and_finance_results.json"),
            JsonSerializer.Serialize(evidenceReport, new JsonSerializerOptions { WriteIndented = true }));
    }
}
