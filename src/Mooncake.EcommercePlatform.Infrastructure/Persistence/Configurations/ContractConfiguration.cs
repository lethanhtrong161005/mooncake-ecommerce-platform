namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Contract"/>.</summary>
public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
            builder.HasKey(e => e.Id).HasName("contracts_pkey");

            builder.ToTable("contracts");

            builder.HasIndex(e => e.ContractNumber, "contracts_contract_number_key").IsUnique();

            builder.HasIndex(e => e.BidId, "idx_contracts_bid_id").HasFilter("(bid_id IS NOT NULL)");

            builder.HasIndex(e => e.CustomerId, "idx_contracts_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.DepositRuleId, "idx_contracts_deposit_rule_id").HasFilter("(deposit_rule_id IS NOT NULL)");

            builder.HasIndex(e => e.OrderId, "idx_contracts_order_id").HasFilter("(order_id IS NOT NULL)");

            builder.HasIndex(e => e.RfqId, "idx_contracts_rfq_id").HasFilter("(rfq_id IS NOT NULL)");

            builder.HasIndex(e => e.SupplierId, "idx_contracts_supplier_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BidId).HasColumnName("bid_id");
            builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
            builder.Property(e => e.ContractNumber)
                .HasMaxLength(50)
                .HasColumnName("contract_number");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.DeliveryDeadline).HasColumnName("delivery_deadline");
            builder.Property(e => e.DepositAmount)
                .HasPrecision(12, 2)
                .HasColumnName("deposit_amount");
            builder.Property(e => e.DepositPaidAt).HasColumnName("deposit_paid_at");
            builder.Property(e => e.DepositRuleId).HasColumnName("deposit_rule_id");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.PenaltyTerms).HasColumnName("penalty_terms");
            builder.Property(e => e.SignedDocumentUrl).HasColumnName("signed_document_url");
            builder.Property(e => e.SignedDocumentSha256).HasMaxLength(64).HasColumnName("signed_document_sha256");
            builder.Property(e => e.RfqId).HasColumnName("rfq_id");
            builder.Property(e => e.SignedByCustomerAt).HasColumnName("signed_by_customer_at");
            builder.Property(e => e.SignedBySupplierAt).HasColumnName("signed_by_supplier_at");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.TermsAndConditions).HasColumnName("terms_and_conditions");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(e => e.TotalValue)
                .HasPrecision(12, 2)
                .HasColumnName("total_value");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_contracts_status");
        builder.HasIndex(e => e.Status, "idx_contracts_pending_deposit");
    }
}
