namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Bid"/>.</summary>
public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
            builder.HasKey(e => e.Id).HasName("bids_pkey");

            builder.ToTable("bids");

            builder.HasIndex(e => e.BidNumber, "bids_bid_number_key").IsUnique();

            builder.HasIndex(e => e.RfqId, "idx_bids_rfq_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.SupplierId, "idx_bids_supplier_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.TotalPrice, "idx_bids_total_price");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BidNumber)
                .HasMaxLength(50)
                .HasColumnName("bid_number");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.EstimatedDeliveryDays).HasColumnName("estimated_delivery_days");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.RfqId).HasColumnName("rfq_id");
            builder.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("submitted_at");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.TotalPrice)
                .HasPrecision(12, 2)
                .HasColumnName("total_price");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.ValidityDays)
                .HasDefaultValue(30)
                .HasColumnName("validity_days");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_bids_status");
    }
}
