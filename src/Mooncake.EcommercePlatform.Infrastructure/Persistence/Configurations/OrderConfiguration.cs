using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.ShopId).HasColumnName("shop_id").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(OrderStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<OrderStatus>());
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(14,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DiscountTotal).HasColumnName("discount_total").HasColumnType("decimal(14,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ShippingFee).HasColumnName("shipping_fee").HasColumnType("decimal(12,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(14,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.DepositRequired).HasColumnName("deposit_required").HasColumnType("decimal(14,2)").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ReceiverName).HasColumnName("receiver_name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.ReceiverPhone).HasColumnName("receiver_phone").IsRequired().HasMaxLength(30);
        builder.Property(x => x.ShippingAddress).HasColumnName("shipping_address").IsRequired();
        builder.Property(x => x.RequiredDeliveryDate).HasColumnName("required_delivery_date").HasColumnType("date");
        builder.Property(x => x.Note).HasColumnName("note");
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

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).IsDescending(false, true).HasDatabaseName("ix_orders_customer");
        builder.HasIndex(x => new { x.ShopId, x.Status }).HasDatabaseName("ix_orders_shop");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_orders_customer");

        builder.HasOne(x => x.Shop)
            .WithMany()
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_orders_shop");
    }
}
