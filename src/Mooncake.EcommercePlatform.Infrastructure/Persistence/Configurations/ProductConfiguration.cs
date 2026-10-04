namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Product"/>.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
            builder.HasKey(e => e.Id).HasName("products_pkey");

            builder.ToTable("products");

            builder.HasIndex(e => e.BasePrice, "idx_products_base_price");

            builder.HasIndex(e => e.CategoryId, "idx_products_category_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.Name, "idx_products_name_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" });

            builder.HasIndex(e => e.ShopId, "idx_products_shop_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BasePrice)
                .HasPrecision(12, 2)
                .HasColumnName("base_price");
            builder.Property(e => e.CategoryId).HasColumnName("category_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.MaxOrderQty).HasColumnName("max_order_qty");
            builder.Property(e => e.MinOrderQty)
                .HasDefaultValue(1)
                .HasColumnName("min_order_qty");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.ShopId).HasColumnName("shop_id");
            builder.Property(e => e.SupportsCustomPackaging)
                .HasDefaultValue(false)
                .HasColumnName("supports_custom_packaging");
            builder.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1536)");
        builder.HasIndex(e => e.Status, "idx_products_status");
    }
}
