using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(x => x.Url).HasColumnName("url").IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.ProductId).IsUnique().HasDatabaseName("uq_product_primary_image").HasFilter("is_primary = true");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_product_images_product");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductImages)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_images_product");
    }
}
