namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="Shop"/>.</summary>
public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
            builder.HasKey(e => e.Id).HasName("shops_pkey");

            builder.ToTable("shops");

            builder.HasIndex(e => e.Slug, "idx_shops_slug");

            builder.HasIndex(e => e.SupplierId, "shops_supplier_id_key").IsUnique();

            builder.HasIndex(e => e.TemplateId, "idx_shops_template_id");

            builder.HasIndex(e => e.Slug, "shops_slug_key").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BannerUrl).HasColumnName("banner_url");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CustomColors)
                .HasColumnType("jsonb")
                .HasColumnName("custom_colors");
            builder.Property(e => e.CustomCss).HasColumnName("custom_css");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.LogoUrl).HasColumnName("logo_url");
            builder.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            builder.Property(e => e.Slug)
                .HasMaxLength(255)
                .HasColumnName("slug");
            builder.Property(e => e.SupplierId).HasColumnName("supplier_id");
            builder.Property(e => e.TemplateId).HasColumnName("template_id");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_shops_status");
    }
}
