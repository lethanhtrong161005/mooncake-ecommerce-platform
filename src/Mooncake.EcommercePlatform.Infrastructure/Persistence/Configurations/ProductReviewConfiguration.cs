namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ProductReview"/>.</summary>
public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
            builder.HasKey(e => e.Id).HasName("product_reviews_pkey");

            builder.ToTable("product_reviews");

            builder.HasIndex(e => e.IsHiddenByAdmin, "idx_prev_admin_hidden").HasFilter("(is_hidden_by_admin = true)");

            builder.HasIndex(e => e.CustomerId, "idx_prev_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => new { e.ProductId, e.LikeCount }, "idx_prev_like_count")
                .IsDescending(false, true)
                .HasFilter("(is_deleted = false)");

            builder.HasIndex(e => new { e.ProductId, e.CreatedAt }, "idx_prev_product_feed")
                .IsDescending(false, true)
                .HasFilter("((is_deleted = false) AND (is_hidden_by_admin = false))");

            builder.HasIndex(e => new { e.ProductId, e.Rating }, "idx_prev_rating").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.ProductVariantId, "idx_prev_variant_id").HasFilter("((product_variant_id IS NOT NULL) AND (is_deleted = false))");

            builder.HasIndex(e => new { e.CustomerId, e.OrderItemId }, "product_reviews_customer_id_order_item_id_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.Content).HasColumnName("content");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.IsHiddenByAdmin)
                .HasDefaultValue(false)
                .HasColumnName("is_hidden_by_admin");
            builder.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            builder.Property(e => e.IsVerifiedPurchase)
                .HasDefaultValue(true)
                .HasColumnName("is_verified_purchase");
            builder.Property(e => e.LikeCount)
                .HasDefaultValue(0)
                .HasColumnName("like_count");
            builder.Property(e => e.MediaUrls)
                .HasColumnType("jsonb")
                .HasColumnName("media_urls");
            builder.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            builder.Property(e => e.ProductId).HasColumnName("product_id");
            builder.Property(e => e.ProductVariantId).HasColumnName("product_variant_id");
            builder.Property(e => e.Rating).HasColumnName("rating");
            builder.Property(e => e.SupplierRepliedAt).HasColumnName("supplier_replied_at");
            builder.Property(e => e.SupplierReply).HasColumnName("supplier_reply");
            builder.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
