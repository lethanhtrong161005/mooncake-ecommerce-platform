using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;
using Mooncake.EcommercePlatform.Infrastructure.Persistence.Converters;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.ContractId).HasColumnName("contract_id");
        builder.Property(x => x.Direction)
            .HasColumnName("direction")
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(DeliveryDirection.SupplierToCustomer)
            .HasConversion(new SnakeCaseEnumConverter<DeliveryDirection>());
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(DeliveryStatus.Pending)
            .HasConversion(new SnakeCaseEnumConverter<DeliveryStatus>());
        builder.Property(x => x.CarrierName).HasColumnName("carrier_name").HasMaxLength(100);
        builder.Property(x => x.TrackingCode).HasColumnName("tracking_code").HasMaxLength(100);
        builder.Property(x => x.DeliveryAddress).HasColumnName("delivery_address").IsRequired();
        builder.Property(x => x.RecipientName).HasColumnName("recipient_name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.RecipientPhone).HasColumnName("recipient_phone").IsRequired().HasMaxLength(30);
        builder.Property(x => x.ScheduledAtUtc).HasColumnName("scheduled_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.ShippedAtUtc).HasColumnName("shipped_at").HasColumnType("timestamptz(3)");
        builder.Property(x => x.DeliveredAtUtc).HasColumnName("delivered_at").HasColumnType("timestamptz(3)");
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

        builder.HasIndex(x => x.OrderId).HasDatabaseName("ix_deliveries_order");
        builder.HasIndex(x => x.ContractId).HasDatabaseName("ix_deliveries_contract");

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_deliveries_order");

        builder.HasOne(x => x.Contract)
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_deliveries_contract");
    }
}
