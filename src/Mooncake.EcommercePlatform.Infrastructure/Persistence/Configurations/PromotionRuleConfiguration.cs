using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class PromotionRuleConfiguration : IEntityTypeConfiguration<PromotionRule>
{
    public void Configure(EntityTypeBuilder<PromotionRule> builder)
    {
        builder.ToTable("promotion_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.DiscountType)
            .HasColumnName("discount_type")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(new SnakeCaseEnumConverter<DiscountType>());
        builder.Property(x => x.MinQuantity).HasColumnName("min_quantity").IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasColumnType("decimal(5,2)");
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(12,2)");
        builder.Property(x => x.FreeQuantity).HasColumnName("free_quantity");
        builder.Property(x => x.StartsAt).HasColumnName("starts_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.EndsAt).HasColumnName("ends_at").HasColumnType("timestamptz(3)");
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

        builder.HasIndex(x => x.ShopId).HasDatabaseName("ix_promotion_rules_shop");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_promotion_rules_product");

        builder.HasOne(x => x.Shop)
            .WithMany()
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_promo_shop");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("fk_promo_product");
    }
}
