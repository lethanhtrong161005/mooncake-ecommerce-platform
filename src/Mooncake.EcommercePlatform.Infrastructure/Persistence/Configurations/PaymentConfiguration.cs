namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Payment"/>.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
            builder.HasKey(e => e.Id).HasName("payments_pkey");

            builder.ToTable("payments");

            builder.HasIndex(e => e.ContractId, "idx_pay_contract_id").HasFilter("(contract_id IS NOT NULL)");

            builder.HasIndex(e => e.CustomerId, "idx_pay_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.MilestoneId, "idx_pay_milestone_id").HasFilter("(milestone_id IS NOT NULL)");

            builder.HasIndex(e => e.OrderId, "idx_pay_order_id").HasFilter("(order_id IS NOT NULL)");

            builder.HasIndex(e => e.PaidAt, "idx_pay_paid_at")
                .IsDescending()
                .HasFilter("(paid_at IS NOT NULL)");

            builder.HasIndex(e => e.PaymentNumber, "payments_payment_number_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Amount)
                .HasPrecision(12, 2)
                .HasColumnName("amount");
            builder.Property(e => e.ContractId).HasColumnName("contract_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.MilestoneId).HasColumnName("milestone_id");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.PaidAt).HasColumnName("paid_at");
            builder.Property(e => e.PaymentNumber)
                .HasMaxLength(50)
                .HasColumnName("payment_number");
            builder.Property(e => e.TransactionId)
                .HasMaxLength(255)
                .HasColumnName("transaction_id");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Method).HasColumnName("method");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_pay_status");
    }
}
