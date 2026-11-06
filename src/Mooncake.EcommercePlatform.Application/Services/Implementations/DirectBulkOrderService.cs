namespace Mooncake.EcommercePlatform.Application.Services.Implementations;

using Mooncake.EcommercePlatform.Application.Common.Constant;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;
using Mooncake.EcommercePlatform.Application.Common.Interfaces;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Services.Interfaces;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Implements enterprise Direct Bulk Order workflows across SLA negotiation, multi-part VNPay, cancellations, PoD disputes, refunds, and simulated payouts.</summary>
public sealed class DirectBulkOrderService(
    IOrderRepository orderRepository,
    IEnterpriseOrderRepository enterpriseRepository,
    IVnPayGateway vnPayGateway,
    IDateTimeProvider clock) : IDirectBulkOrderService
{
    private const string DefaultFrontendReturnUrl = "http://localhost:5173/#/thanh-toan/ket-qua";

    public Task<SupplierOrderDashboardSummaryResponse> GetSupplierOrderDashboardSummaryAsync(Guid userId,
        CancellationToken cancellationToken = default) =>
        enterpriseRepository.GetSupplierOrderDashboardSummaryAsync(userId, cancellationToken);

    private static void ValidateAdminOperationQuery(AdminOperationPageQueryRequest query)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "amountAsc", "amountDesc");
        if (query.MinAmount is < 0 || query.MaxAmount is < 0 || query.MinAmount > query.MaxAmount)
            throw new HttpException(400, "Invalid amount range.", OrderErrorCodes.ValidationError);
    }

    public Task<PagedResult<OrderRefundCaseResponse>> GetAdminRefundCasesPageAsync(
        AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        ValidateAdminOperationQuery(query);
        return enterpriseRepository.GetAdminRefundCasesPageAsync(query, cancellationToken);
    }

    public Task<PagedResult<SupplierPayoutRequestResponse>> GetAdminPayoutRequestsPageAsync(
        AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        ValidateAdminOperationQuery(query);
        return enterpriseRepository.GetAdminPayoutRequestsPageAsync(query, cancellationToken);
    }

    public Task<PagedResult<OrderDeliveryDisputeResponse>> GetAdminDeliveryDisputesPageAsync(
        AdminOperationPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        ValidateAdminOperationQuery(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc");
        return enterpriseRepository.GetAdminDeliveryDisputesPageAsync(query, cancellationToken);
    }

    public async Task<PagedResult<OrderResponse>> GetOrdersPageAsync(Guid userId, bool supplier,
        OrderPageQueryRequest query, CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc", "totalAmountAsc",
            "totalAmountDesc", "deadlineAsc");
        if (query.Status is { Length: > 0 } && query.Status.Any(status => !Enum.IsDefined(status)))
            throw new HttpException(400, "Invalid order status filter.", OrderErrorCodes.ValidationError);

        var page = await enterpriseRepository.QueryOrdersPageAsync(userId, supplier, query, cancellationToken);
        if (page.Items.Count == 0)
            return new PagedResult<OrderResponse>([], page.Page, page.PageSize, page.TotalCount);

        var orders = page.Items;
        var orderIds = orders.Select(order => order.Id).ToArray();
        var packagingByOrder = await orderRepository.GetPackagingSummariesAsync(orderIds, cancellationToken);
        var itemsByOrder = await orderRepository.GetOrderItemsByOrderIdsAsync(orderIds, cancellationToken);
        var pairs = new List<(Order Order, OrderResponse BaseResponse)>(orders.Count);
        foreach (var order in orders)
        {
            packagingByOrder.TryGetValue(order.Id, out var packaging);
            var items = itemsByOrder?.GetValueOrDefault(order.Id, []) ?? [];
            pairs.Add((order, OrderResponseHelper.ToResponse(order, items, packaging.Packaging,
                packaging.RevisionCount)));
        }

        var enriched = await enterpriseRepository.EnrichOrderResponsesBatchAsync(
            pairs, userId, supplier, false, cancellationToken);
        return new PagedResult<OrderResponse>(enriched, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<CursorPagedOrdersResponse> GetOrdersPagedAsync(
        Guid userId, bool supplier, OrderCursorQueryRequest query, CancellationToken cancellationToken = default)
    {
        var (orders, nextCursor, hasMore) = await enterpriseRepository.QueryOrdersCursorAsync(
            userId, supplier, query, cancellationToken);
        if (orders.Count == 0)
            return new CursorPagedOrdersResponse([], null, false, 0);

        var orderIds = orders.Select(o => o.Id).ToArray();
        var packagingByOrder = await orderRepository.GetPackagingSummariesAsync(orderIds, cancellationToken);
        var itemsByOrder = await orderRepository.GetOrderItemsByOrderIdsAsync(orderIds, cancellationToken);

        var pairs = new List<(Order Order, OrderResponse BaseResponse)>(orders.Count);
        foreach (var order in orders)
        {
            packagingByOrder.TryGetValue(order.Id, out var pkgSummary);
            var orderItems = itemsByOrder is not null
                ? itemsByOrder.GetValueOrDefault(order.Id, [])
                : await orderRepository.GetOrderItemsAsync(order.Id, cancellationToken);
            var baseResp = OrderResponseHelper.ToResponse(order, orderItems, pkgSummary.Packaging, pkgSummary.RevisionCount);
            pairs.Add((order, baseResp));
        }

        var enriched = await enterpriseRepository.EnrichOrderResponsesBatchAsync(
            pairs, userId, supplier, false, cancellationToken);
        return new CursorPagedOrdersResponse(enriched, nextCursor, hasMore, enriched.Count);
    }

    public async Task<ShopOrderPolicyResponse> GetShopOrderPolicyAsync(
        Guid shopId, CancellationToken cancellationToken = default)
    {
        _ = await orderRepository.GetShopAsync(shopId, cancellationToken)
            ?? throw new HttpException(404, "Shop was not found.", OrderErrorCodes.ShopUnavailable);
        var (policy, rates) = await enterpriseRepository.GetShopPolicyAndRatesAsync(shopId, cancellationToken);
        return new ShopOrderPolicyResponse(
            shopId,
            policy?.DepositEnabled ?? false,
            policy?.MinimumOrderTotalForDeposit ?? 0m,
            policy?.DepositPercent ?? OrderPolicyDefaults.MinDepositPercent,
            policy?.TaxIncluded ?? true,
            policy?.TaxRate ?? 0m,
            policy?.AllowSelfPickup ?? true,
            policy?.RequireShippingZoneSelection ?? false,
            policy?.Version ?? 1L,
            rates.Select(r => new ShopShippingRateResponse(
                r.Id, r.RegionCode, r.RegionName, r.FeeAmount, r.FreeShippingThreshold, r.EstimatedDays, r.IsSupported)).ToList());
    }

    public async Task<ShopOrderPolicyResponse> GetMyShopOrderPolicyAsync(
        Guid supplierUserId, Guid? shopId, CancellationToken cancellationToken = default)
    {
        var resolvedShopId = await ResolveOwnedShopIdAsync(supplierUserId, shopId, cancellationToken);
        var (policy, rates) = await enterpriseRepository.GetShopPolicyAndRatesAsync(resolvedShopId, cancellationToken);
        return new ShopOrderPolicyResponse(
            resolvedShopId,
            policy?.DepositEnabled ?? false,
            policy?.MinimumOrderTotalForDeposit ?? 0m,
            policy?.DepositPercent ?? OrderPolicyDefaults.MinDepositPercent,
            policy?.TaxIncluded ?? true,
            policy?.TaxRate ?? 0m,
            policy?.AllowSelfPickup ?? true,
            policy?.RequireShippingZoneSelection ?? false,
            policy?.Version ?? 1L,
            rates.Select(r => new ShopShippingRateResponse(
                r.Id, r.RegionCode, r.RegionName, r.FeeAmount, r.FreeShippingThreshold, r.EstimatedDays, r.IsSupported)).ToList());
    }

    public async Task<ShopOrderPolicyResponse> UpsertMyShopOrderPolicyAsync(
        Guid supplierUserId, UpsertShopOrderPolicyRequest request, CancellationToken cancellationToken = default)
    {
        var resolvedShopId = await ResolveOwnedShopIdAsync(supplierUserId, request.ShopId, cancellationToken);
        if (request.DepositPercent is < OrderPolicyDefaults.MinDepositPercent or > OrderPolicyDefaults.MaxDepositPercent)
            throw new HttpException(400, "Deposit percentage must be between 10% and 50%.", OrderErrorCodes.ValidationError);

        return await enterpriseRepository.UpsertShopPolicyAndRatesAsync(resolvedShopId, supplierUserId, request, cancellationToken);
    }

    public async Task<OrderResponse> ProposeDeliveryDateAsync(
        Guid supplierUserId, Guid orderId, ProposeOrderDeliveryDateRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (!await orderRepository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        if (request.ProposedDeliveryDate < DateOnly.FromDateTime(clock.UtcNow))
            throw new HttpException(400, "Proposed delivery date cannot be in the past.", OrderErrorCodes.ValidationError);

        if (!await enterpriseRepository.ProposeDeliveryDateAsync(
                orderId, supplierUserId, request.ProposedDeliveryDate, request.Reason, request.ExpectedVersion, cancellationToken))
        {
            throw new HttpException(409, "Order status or version changed and a delivery date proposal cannot be submitted.", OrderErrorCodes.StateConflict);
        }

        return await BuildEnrichedOrderResponseAsync(orderId, supplierUserId, true, false, cancellationToken);
    }

    public async Task<OrderResponse> RespondDeliveryProposalAsync(
        Guid customerId, Guid orderId, RespondOrderDeliveryProposalRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        if (!await enterpriseRepository.RespondDeliveryProposalAsync(
                orderId, customerId, request.Approve, request.Reason, request.ExpectedVersion, cancellationToken))
        {
            throw new HttpException(409, "Order status or version changed and the delivery proposal can no longer be answered.", OrderErrorCodes.StateConflict);
        }

        return await BuildEnrichedOrderResponseAsync(orderId, customerId, false, false, cancellationToken);
    }

    public async Task<OrderCancellationRequestResponse> RequestCancellationAsync(
        Guid actorUserId, bool supplier, Guid orderId, CreateOrderCancellationRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var authorized = supplier
            ? await orderRepository.IsShopOwnedByUserAsync(order.ShopId, actorUserId, cancellationToken)
            : order.CustomerId == actorUserId;
        if (!authorized)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        var (cancelResponse, _) = await enterpriseRepository.SubmitCancellationRequestAsync(
            order, actorUserId, supplier, request, cancellationToken);
        return cancelResponse;
    }

    public async Task<OrderCancellationRequestResponse> RespondCancellationRequestAsync(
        Guid actorUserId, bool supplier, bool admin, Guid orderId, Guid requestId,
        RespondOrderCancellationRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var authorized = admin || (supplier
            ? await orderRepository.IsShopOwnedByUserAsync(order.ShopId, actorUserId, cancellationToken)
            : order.CustomerId == actorUserId);
        if (!authorized)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        var (cancelResponse, _) = await enterpriseRepository.RespondCancellationRequestAsync(
            order, requestId, actorUserId, supplier, admin, request, cancellationToken);
        return cancelResponse;
    }

    public async Task<VnPayUrlResponse> CreatePartVnPayUrlAsync(
        Guid customerId, Guid orderId, Guid partId, CreateOrderPartVnPayUrlRequest request, string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rawReturnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? DefaultFrontendReturnUrl : request.ReturnUrl.Trim();
        if (!Uri.TryCreate(rawReturnUrl, UriKind.Absolute, out var returnUri) ||
            (returnUri.Scheme != Uri.UriSchemeHttps && returnUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new HttpException(400, "A valid return URL is required.", OrderErrorCodes.ValidationError);
        }

        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        var (payment, part) = await enterpriseRepository.PreparePartVnPayPaymentAsync(customerId, orderId, partId, cancellationToken);
        if (part is null)
            throw new HttpException(404, "Payment part was not found for this order.", OrderErrorCodes.StateConflict);
        if (part.Status == "Paid" || payment?.Status == Domain.Enums.PaymentStatus.Completed)
            throw new HttpException(409, "This payment part has already been paid.", OrderErrorCodes.StateConflict);
        if (payment is null)
            throw new HttpException(409, "This order payment part is not currently payable.", OrderErrorCodes.StateConflict);

        var paymentUrl = vnPayGateway.CreatePaymentUrl(payment, returnUri.AbsoluteUri, ipAddress ?? "127.0.0.1", clock.UtcNow);
        return new VnPayUrlResponse(payment.Id, payment.PaymentNumber, payment.Amount, paymentUrl);
    }

    public async Task<OrderPaymentSummaryResponse> GetPaymentSummaryAsync(
        Guid viewerUserId, bool supplier, bool admin, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await AuthorizeOrderReadAsync(viewerUserId, supplier, admin, orderId, cancellationToken);
        return await enterpriseRepository.GetPaymentSummaryAsync(order, cancellationToken);
    }

    public async Task<OrderFinancialSummaryResponse> GetFinancialSummaryAsync(
        Guid viewerUserId, bool supplier, bool admin, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await AuthorizeOrderReadAsync(viewerUserId, supplier, admin, orderId, cancellationToken);
        return await enterpriseRepository.GetFinancialSummaryAsync(order, cancellationToken);
    }

    public async Task<PagedResult<OrderFinancialLedgerItemResponse>> GetOrderLedgerPageAsync(Guid viewerUserId,
        bool supplier, bool admin, Guid orderId, OrderLedgerPageQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        PagedQueryHelper.Validate(query);
        PagedQueryHelper.ValidateSort(query.Sort, "createdAtDesc", "createdAtAsc");
        _ = await AuthorizeOrderReadAsync(viewerUserId, supplier, admin, orderId, cancellationToken);
        return await enterpriseRepository.GetOrderLedgerPageAsync(orderId, query, cancellationToken);
    }

    public async Task<OrderResponse> ConfirmDeliveryReceiptAsync(
        Guid customerId, Guid orderId, ConfirmOrderDeliveryReceiptRequest? request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        if (!await enterpriseRepository.ConfirmOrderDeliveryWithProofAsync(
                order, customerId, request?.DeliveryId, request?.Notes, cancellationToken))
        {
            throw new HttpException(409, "Only shipped orders with uploaded Proof-of-Delivery photos can be confirmed delivered.", OrderErrorCodes.StateConflict);
        }

        return await BuildEnrichedOrderResponseAsync(orderId, customerId, false, false, cancellationToken);
    }

    public async Task<OrderDeliveryDisputeResponse> ReportDeliveryDisputeAsync(
        Guid customerId, Guid orderId, ReportOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        return await enterpriseRepository.ReportDeliveryDisputeAsync(order, customerId, request, cancellationToken);
    }

    public Task<OrderDeliveryDisputeResponse> ResolveDeliveryDisputeAsync(
        Guid adminUserId, Guid disputeId, ResolveOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default) =>
        enterpriseRepository.ResolveDeliveryDisputeAsync(disputeId, adminUserId, request, cancellationToken);

    public async Task<OrderRefundCaseResponse> RequestRefundAsync(
        Guid customerId, Guid orderId, CreateOrderRefundRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (order.CustomerId != customerId)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        return await enterpriseRepository.CreateRefundCaseAsync(order, customerId, request, cancellationToken);
    }

    public Task<OrderRefundCaseResponse> ProcessRefundCaseAsync(
        Guid adminUserId, Guid refundCaseId, ProcessOrderRefundCaseRequest request, CancellationToken cancellationToken = default) =>
        enterpriseRepository.ProcessRefundCaseAsync(refundCaseId, adminUserId, request, cancellationToken);

    public Task<SupplierPayoutAccountResponse?> GetMyPayoutAccountAsync(
        Guid supplierUserId, CancellationToken cancellationToken = default) =>
        enterpriseRepository.GetSupplierPayoutAccountAsync(supplierUserId, cancellationToken);

    public Task<SupplierPayoutAccountResponse> UpsertMyPayoutAccountAsync(
        Guid supplierUserId, UpsertSupplierPayoutAccountRequest request, CancellationToken cancellationToken = default) =>
        enterpriseRepository.UpsertSupplierPayoutAccountAsync(supplierUserId, request, cancellationToken);

    public Task<SupplierFinancialOverviewResponse> GetMyFinancialOverviewAsync(
        Guid supplierUserId, CancellationToken cancellationToken = default) =>
        enterpriseRepository.GetSupplierFinancialOverviewAsync(supplierUserId, cancellationToken);

    public async Task<SupplierPayoutRequestResponse> CreatePayoutRequestAsync(
        Guid supplierUserId, Guid orderId, CreateOrderPayoutRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        if (!await orderRepository.IsShopOwnedByUserAsync(order.ShopId, supplierUserId, cancellationToken))
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);

        return await enterpriseRepository.CreatePayoutRequestAsync(order, supplierUserId, request, cancellationToken);
    }

    public Task<SupplierPayoutRequestResponse> ProcessPayoutRequestAsync(
        Guid adminUserId, Guid payoutRequestId, ProcessOrderPayoutRequest request, CancellationToken cancellationToken = default) =>
        enterpriseRepository.ProcessPayoutRequestAsync(payoutRequestId, adminUserId, request, cancellationToken);

    public Task<AdminOrderOperationsOverviewResponse> GetAdminOperationsOverviewAsync(CancellationToken cancellationToken = default) =>
        enterpriseRepository.GetAdminOperationsOverviewAsync(cancellationToken);

    private async Task<Guid> ResolveOwnedShopIdAsync(Guid supplierUserId, Guid? requestedShopId, CancellationToken cancellationToken)
    {
        if (requestedShopId.HasValue)
        {
            if (!await orderRepository.IsShopOwnedByUserAsync(requestedShopId.Value, supplierUserId, cancellationToken))
                throw new HttpException(403, "You do not own the specified shop.", OrderErrorCodes.TenantForbidden);
            return requestedShopId.Value;
        }

        var owned = await enterpriseRepository.GetOwnedShopIdsAsync(supplierUserId, cancellationToken);
        if (owned.Count == 0)
            throw new HttpException(404, "No active shop is registered under this supplier account.", OrderErrorCodes.ShopUnavailable);
        return owned[0];
    }

    private async Task<Order> AuthorizeOrderReadAsync(
        Guid viewerUserId, bool supplier, bool admin, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var authorized = admin || (supplier
            ? await orderRepository.IsShopOwnedByUserAsync(order.ShopId, viewerUserId, cancellationToken)
            : order.CustomerId == viewerUserId);
        if (!authorized)
            throw new HttpException(404, B2BOrderingMessages.OrderNotFound, OrderErrorCodes.TenantForbidden);
        return order;
    }

    private async Task<Order> GetOrderOrThrowAsync(Guid orderId, CancellationToken cancellationToken) =>
        await orderRepository.GetOrderAsync(orderId, cancellationToken)
        ?? throw new HttpException(404, B2BOrderingMessages.OrderNotFound);

    private async Task<OrderResponse> BuildEnrichedOrderResponseAsync(
        Guid orderId, Guid viewerUserId, bool supplier, bool admin, CancellationToken cancellationToken)
    {
        var order = await GetOrderOrThrowAsync(orderId, cancellationToken);
        var items = await orderRepository.GetOrderItemsAsync(orderId, cancellationToken);
        var summaries = await orderRepository.GetPackagingSummariesAsync([orderId], cancellationToken);
        summaries.TryGetValue(orderId, out var pkgSummary);
        var baseResponse = OrderResponseHelper.ToResponse(order, items, pkgSummary.Packaging, pkgSummary.RevisionCount);
        return await enterpriseRepository.EnrichOrderResponseAsync(baseResponse, order, viewerUserId, supplier, admin, cancellationToken);
    }
}

