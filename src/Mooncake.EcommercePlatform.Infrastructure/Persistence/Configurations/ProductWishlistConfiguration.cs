namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ProductWishlist"/>.</summary>
public class ProductWishlistConfiguration : IEntityTypeConfiguration<ProductWishlist>
{
    public void Configure(EntityTypeBuilder<ProductWishlist> builder)
    {
        builder.HasKey(e => e.Id).HasName("product_wishlists_pkey");

        builder.ToTable("product_wishlists");

        builder.HasIndex(e => new { e.UserId, e.ProductId }, "uq_product_wishlists_user_product")
            .IsUnique();

        builder.HasIndex(e => new { e.UserId, e.CreatedAt, e.Id }, "idx_product_wishlists_user_cursor")
            .IsDescending(false, true, true);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuid_generate_v4()")
            .HasColumnName("id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ProductId).HasColumnName("product_id");
        builder.Property(e => e.IsDeleted)
            .HasDefaultValue(false)
            .HasColumnName("is_deleted");
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("now()")
            .HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

