namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="RfqItem"/>.</summary>
public class RfqItemConfiguration : IEntityTypeConfiguration<RfqItem>
{
    public void Configure(EntityTypeBuilder<RfqItem> builder)
    {
            builder.HasKey(e => e.Id).HasName("rfq_items_pkey");

            builder.ToTable("rfq_items");

            builder.HasIndex(e => e.RfqId, "idx_rfqi_rfq_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.NeedsCustomPackaging)
                .HasDefaultValue(false)
                .HasColumnName("needs_custom_packaging");
            builder.Property(e => e.PackagingRequirements).HasColumnName("packaging_requirements");
            builder.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
            builder.Property(e => e.Quantity).HasColumnName("quantity");
            builder.Property(e => e.RfqId).HasColumnName("rfq_id");
            builder.Property(e => e.Specifications).HasColumnName("specifications");
            builder.Property(e => e.TargetPrice)
                .HasPrecision(12, 2)
                .HasColumnName("target_price");
            builder.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
