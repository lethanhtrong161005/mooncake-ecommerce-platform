using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.QuotationId).HasColumnName("quotation_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(ContractStatus.Draft)
            .HasConversion(new SnakeCaseEnumConverter<ContractStatus>());
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.DepositPercent).HasColumnName("deposit_percent").HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(30);
        builder.Property(x => x.DeliveryDeadline).HasColumnName("delivery_deadline").HasColumnType("date").IsRequired();
        builder.Property(x => x.LatePenaltyPercentPerDay).HasColumnName("late_penalty_percent_per_day").HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MaxPenaltyPercent).HasColumnName("max_penalty_percent").HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Terms).HasColumnName("terms");
        builder.Property(x => x.CustomerSignedAtUtc).HasColumnName("customer_signed_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.SupplierSignedAtUtc).HasColumnName("supplier_signed_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at").HasColumnType("timestamptz(3)");
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

        builder.HasIndex(x => x.QuotationId).IsUnique().HasDatabaseName("uq_contracts_quotation");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_contracts_customer");
        builder.HasIndex(x => new { x.SupplierId, x.Status }).HasDatabaseName("ix_contracts_supplier");

        builder.HasOne(x => x.Quotation)
            .WithOne()
            .HasForeignKey<Contract>(x => x.QuotationId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_contracts_quotation");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_contracts_customer");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_contracts_supplier");
    }
}
