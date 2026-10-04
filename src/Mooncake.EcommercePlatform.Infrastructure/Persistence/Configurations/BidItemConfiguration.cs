namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="BidItem"/>.</summary>
public class BidItemConfiguration : IEntityTypeConfiguration<BidItem>
{
    public void Configure(EntityTypeBuilder<BidItem> builder)
    {
            builder.HasKey(e => e.Id).HasName("bid_items_pkey");

            builder.ToTable("bid_items");

            builder.HasIndex(e => e.BidId, "idx_bi_bid_id");

            builder.HasIndex(e => e.RfqItemId, "idx_bi_rfq_item_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BidId).HasColumnName("bid_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.Quantity).HasColumnName("quantity");
            builder.Property(e => e.RfqItemId).HasColumnName("rfq_item_id");
            builder.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");
            builder.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
