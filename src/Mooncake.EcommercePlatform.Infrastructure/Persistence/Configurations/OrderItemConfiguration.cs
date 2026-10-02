using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(x => x.VariantId).HasColumnName("variant_id").IsRequired();
        builder.Property(x => x.PromotionRuleId).HasColumnName("promotion_rule_id");
        builder.Property(x => x.CustomPackagingId).HasColumnName("custom_packaging_id");
        builder.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.PackagingFee).HasColumnName("packaging_fee").HasColumnType("decimal(12,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(14,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("decimal(14,2)").IsRequired();
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz(3)")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.OrderId).HasDatabaseName("ix_order_items_order");
        builder.HasIndex(x => x.VariantId).HasDatabaseName("ix_order_items_variant");

        builder.HasOne(x => x.Order)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_oi_order");

        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_oi_variant");

        builder.HasOne(x => x.PromotionRule)
            .WithMany()
            .HasForeignKey(x => x.PromotionRuleId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_oi_promo");

        builder.HasOne(x => x.CustomPackaging)
            .WithMany()
            .HasForeignKey(x => x.CustomPackagingId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_oi_packaging");
    }
}
