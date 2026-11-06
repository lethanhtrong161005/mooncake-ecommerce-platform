namespace Mooncake.EcommercePlatform.Application.Common.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>Persistence contract for enterprise Direct Bulk Order policies, schedules, financial ledgers, disputes, and SLA expiry.</summary>
public interface IEnterpriseOrderRepository
{
    Task<(ShopOrderPolicy? Policy, IReadOnlyList<ShopShippingRate> ShippingRates)> GetShopPolicyAndRatesAsync(
        Guid shopId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetOwnedShopIdsAsync(Guid supplierUserId, CancellationToken cancellationToken = default);

    Task<ShopOrderPolicyResponse> UpsertShopPolicyAndRatesAsync(
        Guid shopId, Guid supplierUserId, UpsertShopOrderPolicyRequest request, CancellationToken cancellationToken = default);

    Task RecordInitialReservationAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default);

    Task<bool> EnsurePaymentScheduleOnConfirmAsync(Order order, Guid actorUserId, DateTime confirmedAtUtc,
        DateOnly? confirmedDeliveryDate, CancellationToken cancellationToken = default);

    Task<bool> ProposeDeliveryDateAsync(Guid orderId, Guid supplierUserId, DateOnly proposedDate, string reason,
        long? expectedVersion, CancellationToken cancellationToken = default);

    Task<bool> RespondDeliveryProposalAsync(Guid orderId, Guid customerId, bool approve, string? reason,
        long? expectedVersion, CancellationToken cancellationToken = default);

    Task<(bool AllowedToStartProduction, bool HasUnapprovedPackaging, bool HasUnpaidPreProductionObligation)> EvaluateProductionReadinessAsync(
        Guid orderId, CancellationToken cancellationToken = default);

    Task<bool> HasUnpaidPreShippingObligationAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task CommitInventoryReservationsAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<OrderResponse> EnrichOrderResponseAsync(OrderResponse baseResponse, Order order, Guid viewerUserId,
        bool viewerIsSupplier, bool viewerIsAdmin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> EnrichOrderResponsesBatchAsync(
        IReadOnlyList<(Order Order, OrderResponse BaseResponse)> items,
        Guid viewerUserId, bool viewerIsSupplier, bool viewerIsAdmin, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Orders, string? NextCursor, bool HasMore)> QueryOrdersCursorAsync(
        Guid userId, bool supplier, OrderCursorQueryRequest query, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> QueryOrdersPageAsync(Guid userId, bool supplier, OrderPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<SupplierOrderDashboardSummaryResponse> GetSupplierOrderDashboardSummaryAsync(Guid userId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<OrderFinancialLedgerItemResponse>> GetOrderLedgerPageAsync(Guid orderId,
        OrderLedgerPageQueryRequest query, CancellationToken cancellationToken = default);

    Task<OrderPaymentSummaryResponse> GetPaymentSummaryAsync(Order order, CancellationToken cancellationToken = default);

    Task<OrderFinancialSummaryResponse> GetFinancialSummaryAsync(Order order, CancellationToken cancellationToken = default);

    Task<(Payment? Payment, OrderPaymentPart? Part)> PreparePartVnPayPaymentAsync(
        Guid customerId, Guid orderId, Guid partId, CancellationToken cancellationToken = default);

    Task<(OrderCancellationRequestResponse Request, OrderStatus UpdatedOrderStatus)> SubmitCancellationRequestAsync(
        Order order, Guid actorUserId, bool isSupplier, CreateOrderCancellationRequest request, CancellationToken cancellationToken = default);

    Task<(OrderCancellationRequestResponse Request, OrderStatus UpdatedOrderStatus)> RespondCancellationRequestAsync(
        Order order, Guid requestId, Guid actorUserId, bool isSupplier, bool isAdmin,
        RespondOrderCancellationRequest request, CancellationToken cancellationToken = default);

    Task<bool> ConfirmOrderDeliveryWithProofAsync(Order order, Guid customerId, Guid? deliveryId, string? notes,
        CancellationToken cancellationToken = default);

    Task<OrderDeliveryDisputeResponse> ReportDeliveryDisputeAsync(Order order, Guid customerId,
        ReportOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default);

    Task<OrderDeliveryDisputeResponse> ResolveDeliveryDisputeAsync(Guid disputeId, Guid adminUserId,
        ResolveOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default);

    Task<OrderRefundCaseResponse> CreateRefundCaseAsync(Order order, Guid customerId,
        CreateOrderRefundRequest request, CancellationToken cancellationToken = default);

    Task<OrderRefundCaseResponse> ProcessRefundCaseAsync(Guid refundCaseId, Guid adminUserId,
        ProcessOrderRefundCaseRequest request, CancellationToken cancellationToken = default);

    Task<SupplierPayoutAccountResponse?> GetSupplierPayoutAccountAsync(Guid supplierUserId, CancellationToken cancellationToken = default);

    Task<SupplierPayoutAccountResponse> UpsertSupplierPayoutAccountAsync(Guid supplierUserId,
        UpsertSupplierPayoutAccountRequest request, CancellationToken cancellationToken = default);

    Task<SupplierFinancialOverviewResponse> GetSupplierFinancialOverviewAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default);

    Task<SupplierPayoutRequestResponse> CreatePayoutRequestAsync(Order order, Guid supplierUserId,
        CreateOrderPayoutRequest request, CancellationToken cancellationToken = default);

    Task<SupplierPayoutRequestResponse> ProcessPayoutRequestAsync(Guid payoutRequestId, Guid adminUserId,
        ProcessOrderPayoutRequest request, CancellationToken cancellationToken = default);

    Task<AdminOrderOperationsOverviewResponse> GetAdminOperationsOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OrderRefundCaseResponse>> GetAdminRefundCasesPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<PagedResult<SupplierPayoutRequestResponse>> GetAdminPayoutRequestsPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDeliveryDisputeResponse>> GetAdminDeliveryDisputesPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);

    Task<int> ExpireOverdueOrdersAsync(CancellationToken cancellationToken = default);
}

