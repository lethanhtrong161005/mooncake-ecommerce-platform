namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ProductVariant"/>.</summary>
public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
            builder.HasKey(e => e.Id).HasName("product_variants_pkey");

            builder.ToTable("product_variants");

            builder.HasIndex(e => e.ProductId, "idx_pv_product_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.StockQty, "idx_pv_stock_qty").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.Sku, "product_variants_sku_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Filling)
                .HasMaxLength(100)
                .HasColumnName("filling");
            builder.Property(e => e.Flavor)
                .HasMaxLength(100)
                .HasColumnName("flavor");
            builder.Property(e => e.ImageUrl).HasColumnName("image_url");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.PriceAdjustment)
                .HasPrecision(12, 2)
                .HasColumnName("price_adjustment");
            builder.Property(e => e.ProductId).HasColumnName("product_id");
            builder.Property(e => e.SizeLabel)
                .HasMaxLength(50)
                .HasColumnName("size_label");
            builder.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasColumnName("sku");
            builder.Property(e => e.StockQty)
                .HasDefaultValue(0)
                .HasColumnName("stock_qty");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.WeightGram).HasColumnName("weight_gram");
    }
}
