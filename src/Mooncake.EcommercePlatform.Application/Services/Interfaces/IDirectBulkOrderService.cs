namespace Mooncake.EcommercePlatform.Application.Services.Interfaces;

using Mooncake.EcommercePlatform.Application.DTOs.Orders.Requests;
using Mooncake.EcommercePlatform.Application.DTOs.Orders.Responses;
using Mooncake.EcommercePlatform.Application.DTOs.Payments.Responses;
using Mooncake.EcommercePlatform.Application.Common.DTOs;

/// <summary>Application workflow contract for enterprise Direct Bulk Order capabilities (SLA negotiation, multi-part VNPay, cancellation matrix, PoD disputes, refunds, and simulated payouts).</summary>
public interface IDirectBulkOrderService
{
    Task<CursorPagedOrdersResponse> GetOrdersPagedAsync(Guid userId, bool supplier, OrderCursorQueryRequest query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderResponse>> GetOrdersPageAsync(Guid userId, bool supplier, OrderPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<SupplierOrderDashboardSummaryResponse> GetSupplierOrderDashboardSummaryAsync(Guid userId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<OrderFinancialLedgerItemResponse>> GetOrderLedgerPageAsync(Guid viewerUserId,
        bool supplier, bool admin, Guid orderId, OrderLedgerPageQueryRequest query,
        CancellationToken cancellationToken = default);

    Task<ShopOrderPolicyResponse> GetShopOrderPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);

    Task<ShopOrderPolicyResponse> GetMyShopOrderPolicyAsync(Guid supplierUserId, Guid? shopId,
        CancellationToken cancellationToken = default);

    Task<ShopOrderPolicyResponse> UpsertMyShopOrderPolicyAsync(Guid supplierUserId, UpsertShopOrderPolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> ProposeDeliveryDateAsync(Guid supplierUserId, Guid orderId,
        ProposeOrderDeliveryDateRequest request, CancellationToken cancellationToken = default);

    Task<OrderResponse> RespondDeliveryProposalAsync(Guid customerId, Guid orderId,
        RespondOrderDeliveryProposalRequest request, CancellationToken cancellationToken = default);

    Task<OrderCancellationRequestResponse> RequestCancellationAsync(Guid actorUserId, bool supplier, Guid orderId,
        CreateOrderCancellationRequest request, CancellationToken cancellationToken = default);

    Task<OrderCancellationRequestResponse> RespondCancellationRequestAsync(Guid actorUserId, bool supplier, bool admin,
        Guid orderId, Guid requestId, RespondOrderCancellationRequest request, CancellationToken cancellationToken = default);

    Task<VnPayUrlResponse> CreatePartVnPayUrlAsync(Guid customerId, Guid orderId, Guid partId,
        CreateOrderPartVnPayUrlRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<OrderPaymentSummaryResponse> GetPaymentSummaryAsync(Guid viewerUserId, bool supplier, bool admin, Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OrderFinancialSummaryResponse> GetFinancialSummaryAsync(Guid viewerUserId, bool supplier, bool admin, Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> ConfirmDeliveryReceiptAsync(Guid customerId, Guid orderId,
        ConfirmOrderDeliveryReceiptRequest? request, CancellationToken cancellationToken = default);

    Task<OrderDeliveryDisputeResponse> ReportDeliveryDisputeAsync(Guid customerId, Guid orderId,
        ReportOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default);

    Task<OrderDeliveryDisputeResponse> ResolveDeliveryDisputeAsync(Guid adminUserId, Guid disputeId,
        ResolveOrderDeliveryDisputeRequest request, CancellationToken cancellationToken = default);

    Task<OrderRefundCaseResponse> RequestRefundAsync(Guid customerId, Guid orderId, CreateOrderRefundRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderRefundCaseResponse> ProcessRefundCaseAsync(Guid adminUserId, Guid refundCaseId,
        ProcessOrderRefundCaseRequest request, CancellationToken cancellationToken = default);

    Task<SupplierPayoutAccountResponse?> GetMyPayoutAccountAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default);

    Task<SupplierPayoutAccountResponse> UpsertMyPayoutAccountAsync(Guid supplierUserId,
        UpsertSupplierPayoutAccountRequest request, CancellationToken cancellationToken = default);

    Task<SupplierFinancialOverviewResponse> GetMyFinancialOverviewAsync(Guid supplierUserId,
        CancellationToken cancellationToken = default);

    Task<SupplierPayoutRequestResponse> CreatePayoutRequestAsync(Guid supplierUserId, Guid orderId,
        CreateOrderPayoutRequest request, CancellationToken cancellationToken = default);

    Task<SupplierPayoutRequestResponse> ProcessPayoutRequestAsync(Guid adminUserId, Guid payoutRequestId,
        ProcessOrderPayoutRequest request, CancellationToken cancellationToken = default);

    Task<AdminOrderOperationsOverviewResponse> GetAdminOperationsOverviewAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OrderRefundCaseResponse>> GetAdminRefundCasesPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<PagedResult<SupplierPayoutRequestResponse>> GetAdminPayoutRequestsPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDeliveryDisputeResponse>> GetAdminDeliveryDisputesPageAsync(AdminOperationPageQueryRequest query,
        CancellationToken cancellationToken = default);
}
