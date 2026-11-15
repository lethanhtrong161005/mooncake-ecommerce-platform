namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="DeliveryItem"/>.</summary>
public class DeliveryItemConfiguration : IEntityTypeConfiguration<DeliveryItem>
{
    public void Configure(EntityTypeBuilder<DeliveryItem> builder)
    {
        builder.HasKey(e => e.Id).HasName("delivery_items_pkey");

        builder.ToTable("delivery_items");

        builder.HasIndex(e => e.DeliveryId, "idx_di_delivery_id");
        builder.HasIndex(e => e.OrderItemId, "idx_di_order_item_id").HasFilter("(order_item_id IS NOT NULL)");
        builder.HasIndex(e => e.RfqItemId, "idx_di_rfq_item_id").HasFilter("(rfq_item_id IS NOT NULL)");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuid_generate_v4()")
            .HasColumnName("id");
        builder.Property(e => e.DeliveryId).HasColumnName("delivery_id");
        builder.Property(e => e.OrderItemId).HasColumnName("order_item_id");
        builder.Property(e => e.RfqItemId).HasColumnName("rfq_item_id");
        builder.Property(e => e.ProductName)
            .HasMaxLength(255)
            .HasColumnName("product_name");
        builder.Property(e => e.Unit)
            .HasMaxLength(50)
            .HasColumnName("unit");
        builder.Property(e => e.Quantity).HasColumnName("quantity");
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false)
            .HasColumnName("is_deleted");
    }
}
