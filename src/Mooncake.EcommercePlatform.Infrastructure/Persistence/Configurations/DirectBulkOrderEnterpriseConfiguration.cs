namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configurations for Direct Bulk Order enterprise entities.</summary>
public static class DirectBulkOrderEnterpriseConfiguration
{
    public static void ConfigureEnterpriseBulkOrder(this ModelBuilder model)
    {
        var policy = model.Entity<ShopOrderPolicy>();
        Names(policy, "shop_order_policies");
        policy.HasKey(x => x.Id);
        policy.Property(x => x.MinimumOrderTotalForDeposit).HasPrecision(14, 2);
        policy.Property(x => x.DepositPercent).HasPrecision(5, 2);
        policy.Property(x => x.TaxRate).HasPrecision(5, 2);
        policy.Property(x => x.Version).HasDefaultValue(1L).IsConcurrencyToken();
        policy.HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        policy.HasIndex(x => x.ShopId).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_shop_order_policies_shop_id");
        policy.ToTable("shop_order_policies", table =>
        {
            table.HasCheckConstraint("ck_shop_order_policies_deposit_pct", "deposit_percent >= 10 AND deposit_percent <= 50");
            table.HasCheckConstraint("ck_shop_order_policies_tax_rate", "tax_rate >= 0 AND tax_rate <= 100");
        });

        var shippingRate = model.Entity<ShopShippingRate>();
        Names(shippingRate, "shop_shipping_rates");
        shippingRate.HasKey(x => x.Id);
        shippingRate.Property(x => x.RegionCode).HasMaxLength(40);
        shippingRate.Property(x => x.RegionName).HasMaxLength(120);
        shippingRate.Property(x => x.FeeAmount).HasPrecision(14, 2);
        shippingRate.Property(x => x.FreeShippingThreshold).HasPrecision(14, 2);
        shippingRate.HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        shippingRate.HasIndex(x => new { x.ShopId, x.RegionCode }).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_shop_shipping_rates_shop_region");
        shippingRate.ToTable("shop_shipping_rates", table =>
            table.HasCheckConstraint("ck_shop_shipping_rates_fee", "fee_amount >= 0 AND estimated_days >= 0"));

        var reservation = model.Entity<InventoryReservation>();
        Names(reservation, "inventory_reservations");
        reservation.HasKey(x => x.Id);
        reservation.Property(x => x.Status).HasMaxLength(30);
        reservation.Property(x => x.ReleaseReason).HasMaxLength(255);
        reservation.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        reservation.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        reservation.HasIndex(x => new { x.OrderId, x.VariantId }).IsUnique()
            .HasDatabaseName("uq_inventory_reservations_order_variant");
        reservation.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("idx_inventory_reservations_status_expires");
        reservation.ToTable("inventory_reservations", table =>
            table.HasCheckConstraint("ck_inventory_reservations_qty", "quantity > 0"));

        var obligation = model.Entity<OrderPaymentObligation>();
        Names(obligation, "order_payment_schedule");
        obligation.HasKey(x => x.Id);
        obligation.Property(x => x.ObligationType).HasMaxLength(40);
        obligation.Property(x => x.Title).HasMaxLength(200);
        obligation.Property(x => x.Status).HasMaxLength(30);
        obligation.Property(x => x.TotalAmount).HasPrecision(14, 2);
        obligation.Property(x => x.PaidAmount).HasPrecision(14, 2);
        obligation.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        obligation.HasIndex(x => new { x.OrderId, x.SequenceNo }).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_order_payment_schedule_order_seq");
        obligation.ToTable("order_payment_schedule", table =>
            table.HasCheckConstraint("ck_order_payment_schedule_amounts", "total_amount > 0 AND paid_amount >= 0"));

        var part = model.Entity<OrderPaymentPart>();
        Names(part, "payment_parts");
        part.HasKey(x => x.Id);
        part.Property(x => x.Status).HasMaxLength(30);
        part.Property(x => x.Amount).HasPrecision(14, 2);
        part.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        part.HasOne<OrderPaymentObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        part.HasIndex(x => new { x.ObligationId, x.PartIndex }).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_payment_parts_obligation_index");
        part.HasIndex(x => x.OrderId).HasDatabaseName("idx_payment_parts_order_id");
        part.ToTable("payment_parts", table =>
            table.HasCheckConstraint("ck_payment_parts_amount", "amount > 0 AND part_index >= 1 AND total_parts >= 1"));

        var feeChange = model.Entity<PackagingFeeChangeRequest>();
        Names(feeChange, "packaging_fee_change_requests");
        feeChange.HasKey(x => x.Id);
        feeChange.Property(x => x.Status).HasMaxLength(40);
        feeChange.Property(x => x.CustomerNotes).HasMaxLength(1000);
        feeChange.Property(x => x.OldPackagingFee).HasPrecision(14, 2);
        feeChange.Property(x => x.DeltaAmount).HasPrecision(14, 2);
        feeChange.Property(x => x.NewPackagingFee).HasPrecision(14, 2);
        feeChange.Property(x => x.OldOrderTotal).HasPrecision(14, 2);
        feeChange.Property(x => x.NewOrderTotal).HasPrecision(14, 2);
        feeChange.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        feeChange.HasOne<CustomPackaging>().WithMany().HasForeignKey(x => x.CustomPackagingId).OnDelete(DeleteBehavior.Restrict);
        feeChange.HasOne<CustomPackagingRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        feeChange.HasIndex(x => x.OrderId).HasDatabaseName("idx_pkg_fee_change_order_id");
        feeChange.HasIndex(x => x.RevisionId).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_pkg_fee_change_revision_id");

        var cancelReq = model.Entity<OrderCancellationRequest>();
        Names(cancelReq, "order_cancellation_requests");
        cancelReq.HasKey(x => x.Id);
        cancelReq.Property(x => x.RequestedByRole).HasMaxLength(20);
        cancelReq.Property(x => x.OrderStatusSnapshot).HasMaxLength(40);
        cancelReq.Property(x => x.Reason).HasMaxLength(1000);
        cancelReq.Property(x => x.Status).HasMaxLength(30);
        cancelReq.Property(x => x.ReviewNotes).HasMaxLength(1000);
        cancelReq.Property(x => x.ProposedRefundAmount).HasPrecision(14, 2);
        cancelReq.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        cancelReq.HasIndex(x => new { x.OrderId, x.Status }).HasDatabaseName("idx_order_cancel_requests_order_status");

        var refundCase = model.Entity<OrderRefundCase>();
        Names(refundCase, "order_refund_cases");
        refundCase.HasKey(x => x.Id);
        refundCase.Property(x => x.CaseNumber).HasMaxLength(50);
        refundCase.Property(x => x.Reason).HasMaxLength(1000);
        refundCase.Property(x => x.Status).HasMaxLength(30);
        refundCase.Property(x => x.AdminNotes).HasMaxLength(1000);
        refundCase.Property(x => x.RefundReferenceCode).HasMaxLength(120);
        refundCase.Property(x => x.RequestedAmount).HasPrecision(14, 2);
        refundCase.Property(x => x.ApprovedAmount).HasPrecision(14, 2);
        refundCase.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        refundCase.HasIndex(x => x.CaseNumber).IsUnique().HasDatabaseName("uq_order_refund_cases_number");
        refundCase.HasIndex(x => x.OrderId).HasDatabaseName("idx_order_refund_cases_order_id");

        var ledger = model.Entity<OrderFinancialLedgerEntry>();
        Names(ledger, "order_financial_ledger_entries");
        ledger.HasKey(x => x.Id);
        ledger.Property(x => x.EntryType).HasMaxLength(40);
        ledger.Property(x => x.BalanceBucket).HasMaxLength(30);
        ledger.Property(x => x.Description).HasMaxLength(500);
        ledger.Property(x => x.IdempotencyKey).HasMaxLength(160);
        ledger.Property(x => x.Amount).HasPrecision(14, 2);
        ledger.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        ledger.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("uq_order_financial_ledger_idempotency");
        ledger.HasIndex(x => new { x.OrderId, x.CreatedAt }).HasDatabaseName("idx_order_financial_ledger_order_created");
        ledger.HasIndex(x => new { x.ShopId, x.BalanceBucket }).HasDatabaseName("idx_order_financial_ledger_shop_bucket");

        var payoutAccount = model.Entity<SupplierPayoutAccount>();
        Names(payoutAccount, "supplier_payout_accounts");
        payoutAccount.HasKey(x => x.Id);
        payoutAccount.Property(x => x.BankCode).HasMaxLength(30);
        payoutAccount.Property(x => x.BankName).HasMaxLength(120);
        payoutAccount.Property(x => x.AccountNumber).HasMaxLength(50);
        payoutAccount.Property(x => x.AccountHolderName).HasMaxLength(150);
        payoutAccount.Property(x => x.BranchName).HasMaxLength(150);
        payoutAccount.Property(x => x.Version).HasDefaultValue(1L).IsConcurrencyToken();
        payoutAccount.HasOne<User>().WithMany().HasForeignKey(x => x.SupplierUserId).OnDelete(DeleteBehavior.Restrict);
        payoutAccount.HasIndex(x => x.SupplierUserId).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_supplier_payout_accounts_user_id");

        var payoutRequest = model.Entity<SupplierPayoutRequest>();
        Names(payoutRequest, "supplier_payout_requests");
        payoutRequest.HasKey(x => x.Id);
        payoutRequest.Property(x => x.RequestNumber).HasMaxLength(50);
        payoutRequest.Property(x => x.Status).HasMaxLength(30);
        payoutRequest.Property(x => x.AdminNotes).HasMaxLength(1000);
        payoutRequest.Property(x => x.PayoutReferenceCode).HasMaxLength(120);
        payoutRequest.Property(x => x.Amount).HasPrecision(14, 2);
        payoutRequest.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        payoutRequest.HasOne<SupplierPayoutAccount>().WithMany().HasForeignKey(x => x.PayoutAccountId).OnDelete(DeleteBehavior.Restrict);
        payoutRequest.HasIndex(x => x.RequestNumber).IsUnique().HasDatabaseName("uq_supplier_payout_requests_number");
        payoutRequest.HasIndex(x => x.OrderId).HasDatabaseName("idx_supplier_payout_requests_order_id");

        var deliveryDispute = model.Entity<OrderDeliveryDispute>();
        Names(deliveryDispute, "order_delivery_disputes");
        deliveryDispute.HasKey(x => x.Id);
        deliveryDispute.Property(x => x.IssueType).HasMaxLength(50);
        deliveryDispute.Property(x => x.Description).HasMaxLength(2000);
        deliveryDispute.Property(x => x.Status).HasMaxLength(30);
        deliveryDispute.Property(x => x.ResolutionNotes).HasMaxLength(1000);
        deliveryDispute.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        deliveryDispute.HasIndex(x => new { x.OrderId, x.Status }).HasDatabaseName("idx_order_delivery_disputes_order_status");
    }

    private static void Names<T>(EntityTypeBuilder<T> entity, string table) where T : class
    {
        entity.ToTable(table);
        foreach (var property in entity.Metadata.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}

