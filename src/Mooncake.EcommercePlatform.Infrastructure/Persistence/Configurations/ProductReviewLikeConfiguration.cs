namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ProductReviewLike"/>.</summary>
public class ProductReviewLikeConfiguration : IEntityTypeConfiguration<ProductReviewLike>
{
    public void Configure(EntityTypeBuilder<ProductReviewLike> builder)
    {
        builder.ToTable("product_review_likes");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ProductReviewId, e.CustomerId }).IsUnique().HasFilter("(is_deleted = false)");
        builder.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasOne<ProductReview>().WithMany().HasForeignKey(e => e.ProductReviewId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CustomerProfile>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
