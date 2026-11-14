namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Implements bulk ordering, server-authoritative pricing/quote versioning, and order status transitions.</summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository repository;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IEnterpriseOrderRepository? enterpriseRepository;
    private readonly INotificationService? notificationService;

    public OrderService(IOrderRepository repository, IDateTimeProvider dateTimeProvider)
        : this(repository, dateTimeProvider, null, null)
    {
    }

    public OrderService(
        IOrderRepository repository,
        IDateTimeProvider dateTimeProvider,
        IEnterpriseOrderRepository? enterpriseRepository)
        : this(repository, dateTimeProvider, enterpriseRepository, null)
    {
    }

    [ActivatorUtilitiesConstructor]
    public OrderService(
        IOrderRepository repository,
        IDateTimeProvider dateTimeProvider,
        IEnterpriseOrderRepository? enterpriseRepository,
        INotificationService? notificationService = null)
    {
        this.repository = repository;
        this.dateTimeProvider = dateTimeProvider;
        this.enterpriseRepository = enterpriseRepository;
        this.notificationService = notificationService;
    }

    public async Task<OrderQuotePreviewResponse> PreviewQuoteAsync(OrderQuotePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is not { Count: >= 1 and <= 100 })
            throw new HttpException(400, "A quote must contain between 1 and 100 items.", OrderErrorCodes.ValidationError);
        if (request.Items.Select(item => item.VariantId).Distinct().Count() != request.Items.Count)
            throw new HttpException(400, "Each product variant can appear only once in a quote.", OrderErrorCodes.ValidationError);
        var shop = await repository.GetShopAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, "Shop was not found.", OrderErrorCodes.ShopUnavailable);
        if (shop.Status != ShopStatus.Active)
            throw new HttpException(409, "The shop is not accepting orders.", OrderErrorCodes.ShopUnavailable);

        var (quote, _, _, _) = await ComputeAuthoritativeQuoteCoreAsync(
            shop, request.Items, request.ShippingZoneCode, false, cancellationToken);
        return quote;
    }

    public async Task<OrderResponse> CreateAsync(Guid customerId, string? idempotencyKey, CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new HttpException(400, "Idempotency-Key header is required.", OrderErrorCodes.ValidationError);
        var normalizedIdempotencyKey = idempotencyKey.Trim();
        if (!Regex.IsMatch(normalizedIdempotencyKey, "^[A-Za-z0-9._:-]{8,128}$", RegexOptions.CultureInvariant))
            throw new HttpException(400, "Idempotency-Key must contain 8 to 128 letters, digits, dots, underscores, colons, or hyphens.", OrderErrorCodes.ValidationError);
        var requestHash = IdempotencyRequestHelper.CreateHash(request);
        var previousOrder = await repository.GetByIdempotencyKeyAsync(customerId, normalizedIdempotencyKey, cancellationToken);
        if (previousOrder is not null)
        {
            if (!string.Equals(previousOrder.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                throw new HttpException(409, "This Idempotency-Key was already used with a different order request.", OrderErrorCodes.IdempotencyConflict);
            return await ToResponseAsync(previousOrder, customerId, false, false, cancellationToken);
        }

        if (request.Items.Select(item => item.VariantId).Distinct().Count() != request.Items.Count)
            throw new HttpException(400, "Each product variant can appear only once in an order.", OrderErrorCodes.ValidationError);
        if (request.DeliveryDateExpected < DateOnly.FromDateTime(dateTimeProvider.UtcNow))
            throw new HttpException(400, "Expected delivery date cannot be in the past.", OrderErrorCodes.ValidationError);

        var shop = await repository.GetShopAsync(request.ShopId, cancellationToken)
                   ?? throw new HttpException(404, "Shop was not found.", OrderErrorCodes.ShopUnavailable);
        if (shop.Status != ShopStatus.Active)
            throw new HttpException(409, "The shop is not accepting orders.", OrderErrorCodes.ShopUnavailable);

        var (quote, products, items, now) = await ComputeAuthoritativeQuoteCoreAsync(
            shop, request.Items, request.ShippingZoneCode, true, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.AcceptedPriceVersion)
            && !string.Equals(request.AcceptedPriceVersion.Trim(), quote.PriceVersion, StringComparison.Ordinal))
        {
            throw new HttpException(
                409,
                "Product pricing, promotions, tax, or shipping policy changed. Review the updated quote before submitting.",
                OrderErrorCodes.PriceChanged,
                quote);
        }

        CustomPackaging? customPackaging = null;
        if (request.CustomPackaging is not null)
        {
            if (!products.All(product => product.SupportsCustomPackaging))
                throw new HttpException(422, B2BOrderingMessages.PackagingNotSupported, OrderErrorCodes.PackagingNotSupported);

            customPackaging = new CustomPackaging
            {
                CompanyName = request.CustomPackaging.CompanyName.Trim(),
                CompanyLogoUrl = CustomPackagingRequestHelper.NormalizeHttpsUrl(request.CustomPackaging.CompanyLogoUrl),
                PackagingType = request.CustomPackaging.PackagingType.Trim(),
                ColorScheme = CustomPackagingRequestHelper.NormalizeColorScheme(request.CustomPackaging.ColorScheme),
                SpecialMessage = request.CustomPackaging.SpecialMessage?.Trim(),
                Notes = request.CustomPackaging.Notes?.Trim(),
                Status = CustomPackagingStatus.Pending,
                CreatedBy = customerId,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        var responseDeadline = now.AddHours(OrderPolicyDefaults.SupplierResponseSlaHours);
        var termsSnapshotJson = JsonSerializer.Serialize(new
        {
            acceptedAtUtc = now,
            acceptedByCustomer = request.AcceptedTerms ?? true,
            supplierResponseSlaHours = OrderPolicyDefaults.SupplierResponseSlaHours,
            depositPaymentSlaHours = OrderPolicyDefaults.DepositPaymentSlaHours,
            customerPodReviewSlaHours = OrderPolicyDefaults.CustomerPodReviewSlaHours,
            mockupReservationBusinessDays = OrderPolicyDefaults.MockupReservationBusinessDays,
            vnPayMaxAmountPerPartVnd = OrderPolicyDefaults.VnPayMaxAmountPerPartVnd,
            priceVersion = quote.PriceVersion
        });

        var order = new Order
        {
            CustomerId = customerId,
            ShopId = shop.Id,
            IdempotencyKey = normalizedIdempotencyKey,
            IdempotencyRequestHash = requestHash,
            OrderNumber = $"MC-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
            Subtotal = quote.Subtotal,
            DiscountAmount = quote.TotalDiscount,
            TaxAmount = quote.TaxAmount,
            ShippingFee = quote.ShippingFee,
            PackagingFee = 0m,
            TotalAmount = quote.PayableTotal,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            RecipientName = request.RecipientName.Trim(),
            RecipientPhone = request.RecipientPhone.Trim(),
            DeliveryDateExpected = request.DeliveryDateExpected,
            Status = OrderStatus.Pending,
            Version = 1L,
            ResponseDeadlineAt = responseDeadline,
            ReservationExpiresAt = responseDeadline,
            PriceSnapshotVersion = quote.PriceVersion,
            ShippingZoneCode = quote.ShippingZoneCode,
            ShippingZoneNameSnapshot = quote.ShippingZoneName,
            TaxIncludedSnapshot = quote.TaxIncluded,
            TaxRateSnapshot = quote.TaxRate,
            DepositRequiredSnapshot = quote.DepositRequired,
            DepositPercentSnapshot = quote.DepositPercent,
            TermsSnapshotJson = termsSnapshotJson,
            CreatedBy = customerId,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = customPackaging is null
            ? await repository.CreateOrderAsync(order, items, cancellationToken)
            : await repository.CreateOrderWithPackagingAsync(order, items, customPackaging, cancellationToken);

        if (!created)
        {
            previousOrder = await repository.GetByIdempotencyKeyAsync(customerId, normalizedIdempotencyKey, cancellationToken);
            if (previousOrder is not null)
            {
                if (!string.Equals(previousOrder.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                    throw new HttpException(409, "This Idempotency-Key was already used with a different order request.", OrderErrorCodes.IdempotencyConflict);
                return await ToResponseAsync(previousOrder, customerId, false, false, cancellationToken);
            }
            throw new HttpException(409, "Inventory changed while placing the order. Review the quantities and try again.", OrderErrorCodes.InsufficientStock);
        }

        if (enterpriseRepository is not null)
            await enterpriseRepository.RecordInitialReservationAsync(order, items, cancellationToken);

        var baseResponse = OrderResponseHelper.ToResponse(order, items, customPackaging, 0);
        return enterpriseRepository is null
            ? baseResponse
            : await enterpriseRepository.EnrichOrderResponseAsync(baseResponse, order, customerId, false, false, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(Guid userId, bool supplier, CancellationToken cancellationToken = default)
    {
        var orders = supplier
            ? await repository.GetSupplierOrdersAsync(userId, cancellationToken)
            : await repository.GetCustomerOrdersAsync(userId, cancellationToken);
        if (orders.Count == 0)
            return [];

        var orderIds = orders.Select(order => order.Id).ToArray();
        var packagingByOrder = await repository.GetPackagingSummariesAsync(orderIds, cancellationToken);
        var itemsByOrder = await repository.GetOrderItemsByOrderIdsAsync(orderIds, cancellationToken);

        var pairs = new List<(Order Order, OrderResponse BaseResponse)>(orders.Count);
        foreach (var order in orders)
        {
            packagingByOrder.TryGetValue(order.Id, out var summary);
            var orderItems = itemsByOrder is not null
                ? itemsByOrder.GetValueOrDefault(order.Id, [])
                : await repository.GetOrderItemsAsync(order.Id, cancellationToken);
            pairs.Add((order, OrderResponseHelper.ToResponse(order, orderItems, summary.Packaging, summary.RevisionCount)));
        }

        if (enterpriseRepository is null)
            return pairs.Select(pair => pair.BaseResponse).ToList();

        return await enterpriseRepository.EnrichOrderResponsesBatchAsync(pairs, userId, supplier, false, cancellationToken);
    }

    public async Task<OrderResponse> GetAsync(Guid userId, bool supplier, Guid orderId, bool admin = false, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        var authorized = admin || (supplier
            ? await repository.IsShopOwnedByUserAsync(order.ShopId, userId, cancellationToken)
            : order.CustomerId == userId);
        if (!authorized)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        return await ToResponseAsync(order, userId, supplier, admin, cancellationToken);
    }

    public async Task<OrderResponse> CancelAsync(Guid customerId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        if (order.Status == OrderStatus.Pending)
        {
            if (!await repository.CancelPendingOrderAsync(order, customerId, request.Reason.Trim(), cancellationToken))
                throw new HttpException(409, "The order status changed and it can no longer be cancelled.", OrderErrorCodes.StateConflict);
            return await ToResponseAsync(order, customerId, false, false, cancellationToken);
        }

        if (enterpriseRepository is not null && (order.Status == OrderStatus.AwaitingCustomerDeliveryApproval || order.Status == OrderStatus.Confirmed))
        {
            var summary = await enterpriseRepository.GetPaymentSummaryAsync(order, cancellationToken);
            if (summary.TotalPaidAmount == 0m)
            {
                await enterpriseRepository.SubmitCancellationRequestAsync(
                    order, customerId, false, new CreateOrderCancellationRequest { Reason = request.Reason.Trim() }, cancellationToken);
                var updated = await repository.GetOrderAsync(orderId, cancellationToken) ?? order;
                return await ToResponseAsync(updated, customerId, false, false, cancellationToken);
            }
        }

        throw new HttpException(409, "Paid or in-production orders require submitting a cancellation request.", OrderErrorCodes.StateConflict);
    }

    public async Task<OrderResponse> ConfirmAsync(Guid supplierUserId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        if (order.Status != OrderStatus.Pending)
            throw new HttpException(409, "Only pending orders can be confirmed.", OrderErrorCodes.StateConflict);
        if (!await repository.ConfirmPendingOrderAsync(order, supplierUserId, cancellationToken))
            throw new HttpException(409, "The order status changed and it can no longer be confirmed.", OrderErrorCodes.StateConflict);

        if (enterpriseRepository is not null)
        {
            await enterpriseRepository.EnsurePaymentScheduleOnConfirmAsync(
                order, supplierUserId, dateTimeProvider.UtcNow, order.DeliveryDateExpected, cancellationToken);
        }

        await NotifyCustomerOrderEventAsync(
            order,
            "OrderConfirmed",
            $"Đơn hàng #{order.OrderNumber} đã được xác nhận",
            "Xưởng đã xác nhận đơn hàng của bạn. Vui lòng kiểm tra lịch thanh toán và tiến độ thực hiện.",
            cancellationToken);

        return await ToResponseAsync(order, supplierUserId, true, false, cancellationToken);
    }

    public async Task<OrderResponse> RejectAsync(Guid supplierUserId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        if (order.Status != OrderStatus.Pending)
            throw new HttpException(409, "Only pending orders can be rejected.", OrderErrorCodes.StateConflict);
        if (!await repository.CancelPendingOrderAsync(order, supplierUserId, $"Supplier rejected: {request.Reason.Trim()}", cancellationToken))
            throw new HttpException(409, "The order status changed and it can no longer be rejected.", OrderErrorCodes.StateConflict);

        await NotifyCustomerOrderEventAsync(
            order,
            "OrderRejected",
            $"Đơn hàng #{order.OrderNumber} đã bị từ chối",
            $"Xưởng đã từ chối đơn hàng. Lý do: {request.Reason.Trim()}",
            cancellationToken);

        return await ToResponseAsync(order, supplierUserId, true, false, cancellationToken);
    }

    public async Task<OrderResponse> StartProcessingAsync(Guid supplierUserId, Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        var packagingStatus = await repository.GetCustomPackagingStatusAsync(orderId, cancellationToken);
        if (packagingStatus.HasValue && packagingStatus != CustomPackagingStatus.Approved)
            throw new HttpException(409, B2BOrderingMessages.PackagingProductionBlocked, OrderErrorCodes.PackagingApprovalRequired);

        if (enterpriseRepository is not null)
        {
            var readiness = await enterpriseRepository.EvaluateProductionReadinessAsync(orderId, cancellationToken);
            if (readiness.HasUnapprovedPackaging)
                throw new HttpException(409, B2BOrderingMessages.PackagingProductionBlocked, OrderErrorCodes.PackagingApprovalRequired);
            if (readiness.HasUnpaidPreProductionObligation)
                throw new HttpException(409, "All pre-production payment obligations (deposit or full order payment and approved packaging fees) must be settled via VNPay before production starts.", OrderErrorCodes.PaymentRequired);
        }

        var response = await TransitionAsSupplierAsync(supplierUserId, order, OrderStatus.Confirmed,
            OrderStatus.Processing, cancellationToken);

        if (enterpriseRepository is not null)
            await enterpriseRepository.CommitInventoryReservationsAsync(orderId, cancellationToken);

        await NotifyCustomerOrderEventAsync(
            order,
            "OrderProcessing",
            $"Đơn hàng #{order.OrderNumber} đã vào sản xuất",
            "Xưởng đã bắt đầu sản xuất đơn hàng theo quy cách của bạn.",
            cancellationToken);

        return response;
    }

    public async Task<OrderResponse> MarkShippedAsync(Guid supplierUserId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (!await repository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        if (enterpriseRepository is not null && await enterpriseRepository.HasUnpaidPreShippingObligationAsync(orderId, cancellationToken))
            throw new HttpException(409, "All remaining balance and supplemental packaging payment obligations must be paid before shipping.", OrderErrorCodes.PaymentRequired);

        var response = await TransitionAsSupplierAsync(supplierUserId, order, OrderStatus.Processing, OrderStatus.Shipped, cancellationToken);

        await NotifyCustomerOrderEventAsync(
            order,
            "OrderShipped",
            $"Đơn hàng #{order.OrderNumber} đang trên đường giao",
            "Xưởng đã xuất kho và bàn giao đơn hàng cho đơn vị vận chuyển.",
            cancellationToken);

        return response;
    }

    public async Task<OrderResponse> ConfirmDeliveryAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetOrderAsync(orderId, cancellationToken)
                    ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        if (order.Status != OrderStatus.Shipped)
            throw new HttpException(409, "Only shipped orders can be marked delivered.", OrderErrorCodes.StateConflict);

        if (enterpriseRepository is not null)
        {
            if (!await enterpriseRepository.ConfirmOrderDeliveryWithProofAsync(order, customerId, null, null, cancellationToken))
                throw new HttpException(409, "The order status changed and delivery could not be confirmed.", OrderErrorCodes.StateConflict);
            return await ToResponseAsync(order, customerId, false, false, cancellationToken);
        }

        if (!await repository.TransitionOrderAsync(orderId, OrderStatus.Shipped, OrderStatus.Delivered, customerId, cancellationToken))
            throw new HttpException(409, "The order status changed and delivery could not be confirmed.", OrderErrorCodes.StateConflict);
        order.Status = OrderStatus.Delivered;
        return await ToResponseAsync(order, customerId, false, false, cancellationToken);
    }

    private async Task<OrderResponse> TransitionAsSupplierAsync(Guid supplierUserId, Order order, OrderStatus expectedStatus,
        OrderStatus nextStatus, CancellationToken cancellationToken)
    {
        if (order.Status != expectedStatus)
            throw new HttpException(409, $"Order must be in {expectedStatus} status before moving to {nextStatus}.", OrderErrorCodes.StateConflict);
        if (!await repository.TransitionOrderAsync(order.Id, expectedStatus, nextStatus, supplierUserId, cancellationToken))
            throw new HttpException(409, "The order status changed concurrently.", OrderErrorCodes.StateConflict);
        order.Status = nextStatus;
        return await ToResponseAsync(order, supplierUserId, true, false, cancellationToken);
    }

    private async Task<OrderResponse> ToResponseAsync(Order order, Guid viewerUserId, bool supplier, bool admin,
        CancellationToken cancellationToken)
    {
        var summaries = await repository.GetPackagingSummariesAsync([order.Id], cancellationToken);
        summaries.TryGetValue(order.Id, out var summary);
        var items = await repository.GetOrderItemsAsync(order.Id, cancellationToken);
        var baseResponse = OrderResponseHelper.ToResponse(order, items, summary.Packaging, summary.RevisionCount);
        return enterpriseRepository is null
            ? baseResponse
            : await enterpriseRepository.EnrichOrderResponseAsync(baseResponse, order, viewerUserId, supplier, admin, cancellationToken);
    }

    private async Task<(OrderQuotePreviewResponse Quote, IReadOnlyList<Product> Products, List<OrderItem> OrderItems, DateTime UtcNow)>
        ComputeAuthoritativeQuoteCoreAsync(
            Shop shop,
            IReadOnlyList<CreateOrderItemRequest> requestedItems,
            string? requestedShippingZoneCode,
            bool enforceZoneRequirement,
            CancellationToken cancellationToken)
    {
        var productIds = requestedItems.Select(item => item.ProductId).Distinct().ToArray();
        var variantIds = requestedItems.Select(item => item.VariantId).Distinct().ToArray();
        var products = await repository.GetProductsAsync(productIds, cancellationToken);
        var variants = await repository.GetVariantsAsync(variantIds, cancellationToken);
        if (products.Count != productIds.Length || variants.Count != variantIds.Length)
            throw new HttpException(400, "One or more selected products or variants are unavailable.", OrderErrorCodes.ValidationError);

        var productsById = products.ToDictionary(product => product.Id);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        var promotions = await repository.GetActivePromotionsAsync(shop.Id, cancellationToken);
        var now = dateTimeProvider.UtcNow;

        var lines = new List<OrderQuoteLineResponse>(requestedItems.Count);
        var orderItems = new List<OrderItem>(requestedItems.Count);
        var versionInputs = new List<(Guid VariantId, int Quantity, decimal OriginalUnitPrice, decimal DiscountedUnitPrice, Guid? PromotionId)>(requestedItems.Count);
        decimal subtotal = 0m;
        decimal totalDiscount = 0m;

        foreach (var item in requestedItems)
        {
            var product = productsById[item.ProductId];
            var variant = variantsById[item.VariantId];
            ValidateOrderLine(shop.Id, product, variant, item.Quantity);

            var pricing = PromotionPricingHelper.Calculate(product.BasePrice + variant.PriceAdjustment,
                item.Quantity, product.Id, promotions, now);
            var lineSubtotal = decimal.Round(pricing.OriginalUnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
            var lineDiscount = pricing.TotalDiscount;
            var lineTotal = lineSubtotal - lineDiscount;

            subtotal += lineSubtotal;
            totalDiscount += lineDiscount;

            lines.Add(new OrderQuoteLineResponse(
                product.Id, variant.Id, product.Name, variant.Name, variant.Sku, item.Quantity,
                pricing.OriginalUnitPrice, pricing.DiscountedUnitPrice, lineSubtotal, lineDiscount, lineTotal,
                pricing.AppliedRule is null
                    ? null
                    : new AppliedPromotionResponse(pricing.AppliedRule.Id, pricing.AppliedRule.Name,
                        pricing.AppliedRule.DiscountType, pricing.AppliedRule.DiscountValue))
            {
                AvailableStock = variant.StockQty
            });

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                VariantId = variant.Id,
                ProductNameSnapshot = product.Name,
                VariantNameSnapshot = variant.Name,
                VariantSkuSnapshot = variant.Sku,
                Quantity = item.Quantity,
                UnitPrice = pricing.OriginalUnitPrice,
                PromotionRuleId = pricing.AppliedRule?.Id,
                DiscountAmount = lineDiscount,
                Subtotal = lineTotal
            });

            versionInputs.Add((variant.Id, item.Quantity, pricing.OriginalUnitPrice, pricing.DiscountedUnitPrice, pricing.AppliedRule?.Id));
        }

        subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        totalDiscount = decimal.Round(totalDiscount, 2, MidpointRounding.AwayFromZero);
        var merchandiseAfterDiscount = subtotal - totalDiscount;

        ShopOrderPolicy? policy = null;
        IReadOnlyList<ShopShippingRate> shippingRates = [];
        if (enterpriseRepository is not null)
        {
            (policy, shippingRates) = await enterpriseRepository.GetShopPolicyAndRatesAsync(shop.Id, cancellationToken);
        }

        string? resolvedZoneCode = null;
        string? resolvedZoneName = null;
        decimal shippingFee = 0m;

        var supportedRates = shippingRates.Where(r => r.IsSupported).ToList();
        if (!string.IsNullOrWhiteSpace(requestedShippingZoneCode))
        {
            var code = requestedShippingZoneCode.Trim().ToUpperInvariant();
            var matched = shippingRates.FirstOrDefault(r => string.Equals(r.RegionCode, code, StringComparison.OrdinalIgnoreCase));
            if (matched is null || !matched.IsSupported)
                throw new HttpException(422, $"Shipping region '{code}' is not supported by this shop.", OrderErrorCodes.ValidationError);

            resolvedZoneCode = matched.RegionCode;
            resolvedZoneName = matched.RegionName;
            shippingFee = matched.FreeShippingThreshold.HasValue && merchandiseAfterDiscount >= matched.FreeShippingThreshold.Value
                ? 0m
                : matched.FeeAmount;
        }
        else if (policy?.RequireShippingZoneSelection == true && supportedRates.Count > 0 && enforceZoneRequirement)
        {
            throw new HttpException(400, "Please select a supported shipping region for this shop.", OrderErrorCodes.ValidationError);
        }
        else if (supportedRates.Count > 0)
        {
            var defaultRate = supportedRates[0];
            resolvedZoneCode = defaultRate.RegionCode;
            resolvedZoneName = defaultRate.RegionName;
            shippingFee = defaultRate.FreeShippingThreshold.HasValue && merchandiseAfterDiscount >= defaultRate.FreeShippingThreshold.Value
                ? 0m
                : defaultRate.FeeAmount;
        }

        var taxIncluded = policy?.TaxIncluded ?? true;
        var taxRate = policy?.TaxRate ?? 0m;
        var taxAmount = !taxIncluded && taxRate > 0m
            ? decimal.Round(merchandiseAfterDiscount * taxRate / 100m, 0, MidpointRounding.AwayFromZero)
            : 0m;

        var payableTotal = decimal.Round(merchandiseAfterDiscount + shippingFee + taxAmount, 2, MidpointRounding.AwayFromZero);

        var depositRequired = policy is { DepositEnabled: true }
            && payableTotal >= policy.MinimumOrderTotalForDeposit
            && policy.DepositPercent is >= OrderPolicyDefaults.MinDepositPercent and <= OrderPolicyDefaults.MaxDepositPercent;
        var depositPercent = depositRequired ? policy!.DepositPercent : 0m;
        var depositAmount = depositRequired
            ? decimal.Round(payableTotal * depositPercent / 100m, 0, MidpointRounding.AwayFromZero)
            : payableTotal;
        var balanceBeforeShipping = depositRequired ? Math.Max(0m, payableTotal - depositAmount) : 0m;

        var priceVersion = PriceQuoteVersionHelper.ComputeVersion(
            shop.Id, versionInputs, taxIncluded, taxRate, depositRequired, depositPercent, resolvedZoneCode, shippingFee);

        var initialParts = PriceQuoteVersionHelper.SplitIntoParts(depositAmount, OrderPolicyDefaults.VnPayMaxAmountPerPartVnd).Count;
        var effectiveRate = subtotal == 0m
            ? 0m
            : decimal.Round(totalDiscount / subtotal * 100m, 2, MidpointRounding.AwayFromZero);

        var quote = new OrderQuotePreviewResponse(subtotal, totalDiscount, merchandiseAfterDiscount, effectiveRate, lines)
        {
            PriceVersion = priceVersion,
            TaxIncluded = taxIncluded,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            ShippingZoneCode = resolvedZoneCode,
            ShippingZoneName = resolvedZoneName,
            ShippingFee = shippingFee,
            PayableTotal = payableTotal,
            DepositRequired = depositRequired,
            DepositPercent = depositPercent,
            DepositAmount = depositAmount,
            BalanceBeforeShippingAmount = balanceBeforeShipping,
            VnPayMaxAmountPerPart = OrderPolicyDefaults.VnPayMaxAmountPerPartVnd,
            EstimatedInitialPaymentParts = Math.Max(1, initialParts),
            AvailableShippingRates = shippingRates.Select(r => new ShopShippingRateResponse(
                r.Id, r.RegionCode, r.RegionName, r.FeeAmount, r.FreeShippingThreshold, r.EstimatedDays, r.IsSupported)).ToList()
        };

        return (quote, products, orderItems, now);
    }

    private static void ValidateOrderLine(Guid shopId, Product product, ProductVariant variant, int quantity)
    {
        if (product.ShopId != shopId || variant.ProductId != product.Id || variant.ShopId != shopId)
            throw new HttpException(400, "All order items must belong to the selected shop and product.", OrderErrorCodes.ValidationError);
        if (product.Status != ProductStatus.Active)
            throw new HttpException(409, $"Product '{product.Name}' is not active.", OrderErrorCodes.ShopUnavailable);
        if (quantity < product.MinOrderQty)
            throw new HttpException(400, $"Product '{product.Name}' requires a minimum quantity of {product.MinOrderQty}.", OrderErrorCodes.ValidationError);
        if (product.MaxOrderQty.HasValue && quantity > product.MaxOrderQty.Value)
            throw new HttpException(400, $"Product '{product.Name}' allows a maximum quantity of {product.MaxOrderQty.Value}.", OrderErrorCodes.ValidationError);
        if (variant.StockQty < quantity)
            throw new HttpException(409, $"Variant '{variant.Name}' has only {variant.StockQty} units available.", OrderErrorCodes.InsufficientStock);
    }

    private async Task NotifyCustomerOrderEventAsync(
        Order order,
        string type,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        if (notificationService is null)
            return;

        try
        {
            await notificationService.DispatchNotificationAsync(
                order.CustomerId,
                type,
                title,
                content,
                referenceType: "Order",
                referenceId: order.Id,
                actionUrl: "/don-hang-cua-toi",
                priority: "high",
                ct: cancellationToken);
        }
        catch
        {
            // Order transitions must stay committed even if realtime notification dispatch fails.
        }
    }
}
