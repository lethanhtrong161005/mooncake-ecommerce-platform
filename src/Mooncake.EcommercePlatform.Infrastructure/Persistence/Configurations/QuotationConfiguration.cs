using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.RfqId).HasColumnName("rfq_id").IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(QuotationStatus.Submitted)
            .HasConversion(new SnakeCaseEnumConverter<QuotationStatus>());
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.LeadTimeDays).HasColumnName("lead_time_days");
        builder.Property(x => x.ProposedDepositPercent).HasColumnName("proposed_deposit_percent").HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(30);
        builder.Property(x => x.ValidUntilUtc).HasColumnName("valid_until").HasColumnType("timestamptz(3)");
        builder.Property(x => x.Notes).HasColumnName("notes");
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

        builder.HasIndex(x => new { x.RfqId, x.SupplierId }).IsUnique().HasDatabaseName("uq_quotations_rfq_supplier");
        builder.HasIndex(x => x.RfqId).IsUnique().HasDatabaseName("uq_one_accepted_quotation_per_rfq").HasFilter("status = 'accepted'");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("ix_quotations_supplier");

        builder.HasOne(x => x.RequestForQuotation)
            .WithMany(x => x.Quotations)
            .HasForeignKey(x => x.RfqId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_quotations_rfq");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_quotations_supplier");
    }
}
