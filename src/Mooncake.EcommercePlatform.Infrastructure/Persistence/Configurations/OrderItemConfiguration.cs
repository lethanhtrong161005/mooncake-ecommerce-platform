namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="OrderItem"/>.</summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
            builder.HasKey(e => e.Id).HasName("order_items_pkey");

            builder.ToTable("order_items");

            builder.HasIndex(e => e.OrderId, "idx_oi_order_id");

            builder.HasIndex(e => e.ProductId, "idx_oi_product_id");

            builder.HasIndex(e => e.PromotionRuleId, "idx_oi_promotion_rule_id").HasFilter("(promotion_rule_id IS NOT NULL)");

            builder.HasIndex(e => e.VariantId, "idx_oi_variant_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.DiscountAmount)
                .HasPrecision(12, 2)
                .HasColumnName("discount_amount");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.ProductId).HasColumnName("product_id");
            builder.Property(e => e.ProductNameSnapshot).HasMaxLength(255).HasDefaultValue("").HasColumnName("product_name_snapshot");
            builder.Property(e => e.VariantNameSnapshot).HasMaxLength(255).HasDefaultValue("").HasColumnName("variant_name_snapshot");
            builder.Property(e => e.VariantSkuSnapshot).HasMaxLength(100).HasDefaultValue("").HasColumnName("variant_sku_snapshot");
            builder.Property(e => e.PromotionRuleId).HasColumnName("promotion_rule_id");
            builder.Property(e => e.Quantity).HasColumnName("quantity");
            builder.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");
            builder.Property(e => e.PackagingFee).HasPrecision(12, 2).HasDefaultValue(0).HasColumnName("packaging_fee");
            builder.Property(e => e.UnitPrice)
                .HasPrecision(12, 2)
                .HasColumnName("unit_price");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.VariantId).HasColumnName("variant_id");
    }
}
