namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="PromotionRule"/>.</summary>
public class PromotionRuleConfiguration : IEntityTypeConfiguration<PromotionRule>
{
    public void Configure(EntityTypeBuilder<PromotionRule> builder)
    {
            builder.HasKey(e => e.Id).HasName("promotion_rules_pkey");

            builder.ToTable("promotion_rules");

            builder.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate }, "idx_pr_active").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.ProductId, "idx_pr_product_id").HasFilter("((product_id IS NOT NULL) AND (is_deleted = false))");

            builder.HasIndex(e => e.ShopId, "idx_pr_shop_id").HasFilter("(is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.DiscountValue)
                .HasPrecision(12, 2)
                .HasColumnName("discount_value");
            builder.Property(e => e.EndDate).HasColumnName("end_date");
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.MaxQty).HasColumnName("max_qty");
            builder.Property(e => e.MinQty).HasColumnName("min_qty");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.ProductId).HasColumnName("product_id");
            builder.Property(e => e.ShopId).HasColumnName("shop_id");
            builder.Property(e => e.StartDate).HasColumnName("start_date");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.DiscountType).HasColumnName("discount_type");
    }
}
