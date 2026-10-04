namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using Mooncake.EcommercePlatform.Domain.Enums;

/// <summary>EF Core configuration for <see cref="CustomPackaging"/>.</summary>
public class CustomPackagingConfiguration : IEntityTypeConfiguration<CustomPackaging>
{
    public void Configure(EntityTypeBuilder<CustomPackaging> builder)
    {
            builder.HasKey(e => e.Id).HasName("custom_packagings_pkey");

            builder.ToTable("custom_packagings");

            builder.HasIndex(e => e.OrderId, "idx_cpkg_order_id");

            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            builder.Property(e => e.BoxDesignUrl).HasColumnName("box_design_url");
            builder.Property(e => e.ColorScheme)
                .HasColumnType("jsonb")
                .HasColumnName("color_scheme");
            builder.Property(e => e.CompanyLogoUrl).HasColumnName("company_logo_url");
            builder.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("company_name");
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.OrderId).HasColumnName("order_id");
            builder.Property(e => e.PackagingType)
                .HasMaxLength(100)
                .HasColumnName("packaging_type");
            builder.Property(e => e.SpecialMessage).HasColumnName("special_message");
            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.HasIndex(e => e.Status, "idx_cpkg_status");
    }
}
