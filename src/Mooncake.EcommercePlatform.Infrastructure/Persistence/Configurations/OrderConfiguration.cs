namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Order"/>.</summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
            builder.HasKey(e => e.Id).HasName("orders_pkey");

            builder.ToTable("orders");

            builder.HasIndex(e => e.CreatedAt, "idx_orders_created_at").IsDescending();

            builder.HasIndex(e => e.CustomerId, "idx_orders_customer_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.ShopId, "idx_orders_shop_id").HasFilter("(is_deleted = false)");

            builder.HasIndex(e => e.OrderNumber, "orders_order_number_key").IsUnique();

            builder.HasIndex(e => new { e.CustomerId, e.IdempotencyKey }, "orders_customer_idempotency_key_key")
                .IsUnique()
                .HasFilter("(idempotency_key IS NOT NULL AND is_deleted = false)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomerId).HasColumnName("customer_id");
            builder.Property(e => e.DeliveryAddress).HasColumnName("delivery_address");
            builder.Property(e => e.RecipientName).HasMaxLength(255).HasColumnName("recipient_name");
            builder.Property(e => e.RecipientPhone).HasMaxLength(20).HasColumnName("recipient_phone");
            builder.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            builder.Property(e => e.CancelledByUserId).HasColumnName("cancelled_by_user_id");
            builder.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            builder.Property(e => e.DeliveryDateExpected).HasColumnName("delivery_date_expected");
            builder.Property(e => e.DiscountAmount)
                .HasPrecision(12, 2)
                .HasColumnName("discount_amount");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            builder.Property(e => e.IdempotencyKey).HasMaxLength(128).HasColumnName("idempotency_key");
            builder.Property(e => e.IdempotencyRequestHash).HasMaxLength(64).HasColumnName("idempotency_request_hash");
            builder.Property(e => e.ShippingFee)
                .HasPrecision(12, 2)
                .HasColumnName("shipping_fee");
            builder.Property(e => e.ShopId).HasColumnName("shop_id");
            builder.Property(e => e.Subtotal)
                .HasPrecision(12, 2)
                .HasColumnName("subtotal");
            builder.Property(e => e.TaxAmount)
                .HasPrecision(12, 2)
                .HasColumnName("tax_amount");
            builder.Property(e => e.TotalAmount)
                .HasPrecision(12, 2)
                .HasColumnName("total_amount");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_orders_status");
    }
}
