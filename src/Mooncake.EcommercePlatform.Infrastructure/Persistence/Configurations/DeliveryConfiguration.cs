namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Delivery"/>.</summary>
public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
            builder.HasKey(e => e.Id).HasName("deliveries_pkey");

            builder.ToTable("deliveries");

            builder.HasIndex(e => e.DeliveryNumber, "deliveries_delivery_number_key").IsUnique();

            builder.HasIndex(e => e.ContractId, "idx_del_contract_id").HasFilter("(contract_id IS NOT NULL)");

            builder.HasIndex(e => e.OrderId, "idx_del_order_id").HasFilter("(order_id IS NOT NULL)");

            builder.HasIndex(e => e.TrackingCode, "idx_del_tracking_code").HasFilter("(tracking_code IS NOT NULL)");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.ActualDeliveryDate).HasColumnName("actual_delivery_date");
            builder.Property(e => e.ContractId).HasColumnName("contract_id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.DeliveryNumber)
                .HasMaxLength(50)
                .HasColumnName("delivery_number");
            builder.Property(e => e.FromAddress).HasColumnName("from_address");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.RecipientName)
                .HasMaxLength(255)
                .HasColumnName("recipient_name");
            builder.Property(e => e.RecipientPhone)
                .HasMaxLength(20)
                .HasColumnName("recipient_phone");
            builder.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            builder.Property(e => e.ToAddress).HasColumnName("to_address");
            builder.Property(e => e.TrackingCode)
                .HasMaxLength(100)
                .HasColumnName("tracking_code");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.DeliveryType).HasColumnName("delivery_type");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_del_status");
    }
}
