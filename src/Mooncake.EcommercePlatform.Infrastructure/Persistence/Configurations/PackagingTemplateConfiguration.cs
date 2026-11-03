namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

public class PackagingTemplateConfiguration : IEntityTypeConfiguration<PackagingTemplate>
{
    public void Configure(EntityTypeBuilder<PackagingTemplate> builder)
    {
        builder.HasKey(e => e.Id).HasName("packaging_templates_pkey");
        builder.ToTable("packaging_templates");

        builder.HasIndex(e => e.ShopId, "idx_packaging_templates_shop_id");

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()")
            .HasColumnName("id");
        builder.Property(e => e.ShopId).HasColumnName("shop_id");
        builder.Property(e => e.Name).HasMaxLength(255).HasColumnName("name");
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Material).HasMaxLength(150).HasColumnName("material");
        builder.Property(e => e.Dimensions).HasMaxLength(100).HasColumnName("dimensions");
        builder.Property(e => e.Capacity).HasColumnName("capacity");
        builder.Property(e => e.BasePrice).HasColumnType("numeric(12, 2)").HasColumnName("base_price");
        builder.Property(e => e.SampleImageUrl).HasColumnName("sample_image_url");
        builder.Property(e => e.Technique).HasMaxLength(150).HasColumnName("technique");
        builder.Property(e => e.MinOrderQty).HasColumnName("min_order_qty");
        builder.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
        builder.Property(e => e.SortOrder).HasDefaultValue(0).HasColumnName("sort_order");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).HasColumnName("is_deleted");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}

