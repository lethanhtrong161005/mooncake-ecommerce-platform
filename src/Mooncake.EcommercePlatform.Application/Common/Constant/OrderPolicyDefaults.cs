namespace Mooncake.EcommercePlatform.Application.Common.Constant;

/// <summary>Authoritative SLA, payment cap, reservation, and feature-flag defaults for enterprise bulk orders.</summary>
public static class OrderPolicyDefaults
{
    public const string SupplierResponseDeadlineHoursKey = "orders.supplier_response_deadline_hours";
    public const int DefaultSupplierResponseDeadlineHours = 12;
    public const int SupplierResponseSlaHours = DefaultSupplierResponseDeadlineHours;

    public const string CustomerDeliveryApprovalHoursKey = "orders.customer_delivery_approval_hours";
    public const int DefaultCustomerDeliveryApprovalHours = 12;
    public const int CustomerDeliveryProposalSlaHours = DefaultCustomerDeliveryApprovalHours;

    public const string DepositPaymentDeadlineHoursKey = "orders.deposit_payment_deadline_hours";
    public const int DefaultDepositPaymentDeadlineHours = 24;
    public const int DepositPaymentSlaHours = DefaultDepositPaymentDeadlineHours;

    public const string PodReviewDeadlineHoursKey = "orders.pod_review_deadline_hours";
    public const int DefaultPodReviewDeadlineHours = 48;
    public const int CustomerPodReviewSlaHours = DefaultPodReviewDeadlineHours;

    public const string PackagingReservationBusinessDaysKey = "orders.packaging_reservation_business_days";
    public const int DefaultPackagingReservationBusinessDays = 5;
    public const int MockupReservationBusinessDays = DefaultPackagingReservationBusinessDays;

    public const string VnPayMaxPartAmountVndKey = "payments.vnpay_max_part_amount_vnd";
    public const decimal DefaultVnPayMaxPartAmountVnd = 100_000_000m;
    public const decimal VnPayMaxAmountPerPartVnd = DefaultVnPayMaxPartAmountVnd;

    public const string RefundInitiationBusinessDaysKey = "orders.refund_initiation_business_days";
    public const int DefaultRefundInitiationBusinessDays = 3;

    public const string QuotePreviewTtlMinutesKey = "orders.quote_preview_ttl_minutes";
    public const int DefaultQuotePreviewTtlMinutes = 15;

    public const string DirectOrderBankTransferEnabledKey = "orders.direct_bank_transfer_enabled";
    public const bool DefaultDirectOrderBankTransferEnabled = false;

    public const string DirectOrderCodEnabledKey = "orders.direct_cod_enabled";
    public const bool DefaultDirectOrderCodEnabled = false;

    public const string ProductionPayoutEnabledKey = "orders.production_payout_enabled";
    public const bool DefaultProductionPayoutEnabled = false;
    public const bool ProductionPayoutEnabledDefault = DefaultProductionPayoutEnabled;

    public const decimal MinDepositPercent = 10m;
    public const decimal MaxDepositPercent = 50m;
    public const decimal DefaultDepositPercent = 30m;
    public const decimal DefaultVatRatePercent = 8m;
}
