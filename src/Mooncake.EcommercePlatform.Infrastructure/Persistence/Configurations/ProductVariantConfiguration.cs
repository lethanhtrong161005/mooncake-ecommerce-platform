using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.StockQuantity).HasColumnName("stock_quantity").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity").IsRequired().HasDefaultValue(1);
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
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

        builder.HasIndex(x => x.Sku).IsUnique().HasDatabaseName("uq_variants_sku").HasFilter("sku IS NOT NULL");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_variants_product");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductVariants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_variants_product");
    }
}
