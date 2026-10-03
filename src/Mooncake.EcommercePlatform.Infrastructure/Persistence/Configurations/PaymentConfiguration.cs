using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.MilestoneId).HasColumnName("milestone_id");
        builder.Property(x => x.PaymentType)
            .HasColumnName("payment_type")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<PaymentType>());
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.Method)
            .HasColumnName("method")
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion(new SnakeCaseEnumConverter<PaymentMethod>());
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<PaymentStatus>());
        builder.Property(x => x.TransactionRef).HasColumnName("transaction_ref").HasMaxLength(100);
        builder.Property(x => x.PaidAtUtc).HasColumnName("paid_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.TransactionRef).IsUnique().HasDatabaseName("uq_payments_transaction_ref").HasFilter("transaction_ref IS NOT NULL");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("ix_payments_order");
        builder.HasIndex(x => x.MilestoneId).HasDatabaseName("ix_payments_milestone");

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_payments_order");

        builder.HasOne(x => x.Milestone)
            .WithMany()
            .HasForeignKey(x => x.MilestoneId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_payments_milestone");
    }
}
