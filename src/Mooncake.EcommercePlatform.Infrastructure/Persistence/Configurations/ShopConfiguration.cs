using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("shops");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.TemplateOverrides)
            .HasColumnName("template_overrides")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Slug).HasColumnName("slug").IsRequired().HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.LogoUrl).HasColumnName("logo_url").HasMaxLength(1000);
        builder.Property(x => x.BannerUrl).HasColumnName("banner_url").HasMaxLength(1000);
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
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

        builder.HasIndex(x => x.SupplierId).IsUnique().HasDatabaseName("uq_shops_supplier");
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("uq_shops_slug");
        builder.HasIndex(x => x.TemplateId).HasDatabaseName("ix_shops_template");

        builder.HasOne(x => x.Supplier)
            .WithOne()
            .HasForeignKey<Shop>(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shops_supplier");

        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_shops_template");
    }
}
