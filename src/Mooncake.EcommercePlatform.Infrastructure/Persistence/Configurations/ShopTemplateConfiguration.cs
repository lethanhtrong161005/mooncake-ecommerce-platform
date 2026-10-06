namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>EF Core configuration for <see cref="ShopTemplate"/>.</summary>
public class ShopTemplateConfiguration : IEntityTypeConfiguration<ShopTemplate>
{
    public void Configure(EntityTypeBuilder<ShopTemplate> builder)
    {
            builder.HasKey(e => e.Id).HasName("shop_templates_pkey");

            builder.ToTable("shop_templates");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CssVariables)
                .HasColumnType("jsonb")
                .HasColumnName("css_variables");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            builder.Property(e => e.IsPremium)
                .HasDefaultValue(false)
                .HasColumnName("is_premium");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.LayoutConfig)
                .HasColumnType("jsonb")
                .HasColumnName("layout_config");
            builder.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            builder.Property(e => e.PreviewImageUrl).HasColumnName("preview_image_url");
            builder.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}
