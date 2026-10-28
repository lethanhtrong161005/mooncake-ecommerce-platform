namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Shop aggregates, immutable templates and tenant-safe media references.</summary>
public static class ShopFeatureConfiguration
{
    public static void ConfigureShops(this ModelBuilder model)
    {
        model.Entity<Product>().HasIndex(x => new { x.ShopId, x.CreatedAt, x.Id }).HasDatabaseName("idx_shop_public_product_feed")
            .IsDescending(false, true, false).HasFilter("is_deleted = false AND status IN ('Active','OutOfStock')");
        var shop = model.Entity<Shop>(); Names(shop, "shops");
        shop.Property(x => x.Version).HasDefaultValue(1L).IsConcurrencyToken();
        shop.Property(x => x.Name).IsRequired().HasMaxLength(255);
        shop.Property(x => x.Slug).HasMaxLength(80);
        shop.Property(x => x.Description).HasMaxLength(2000);
        shop.Property(x => x.PublicContactEmail).HasMaxLength(255);
        shop.Property(x => x.PublicContactPhone).HasMaxLength(20);
        shop.Property(x => x.PublicAddress).HasMaxLength(500);
        shop.Property(x => x.RegionCode).HasMaxLength(20);
        shop.Property(x => x.SuspensionReason).HasMaxLength(1000);
        shop.Property(x => x.CustomColors).HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        shop.HasOne<SupplierProfile>().WithMany().HasForeignKey(x => new { x.SupplierId, x.OwnerUserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId }).HasConstraintName("fk_shop_supplier_owner").OnDelete(DeleteBehavior.Restrict);
        model.Entity<SupplierProfile>().HasAlternateKey(x => new { x.Id, x.UserId }).HasName("uq_sv_profile_owner");
        shop.HasOne<ShopTemplateRevision>().WithMany().HasForeignKey(x => new { x.TemplateId, x.TemplateRevisionId })
            .HasPrincipalKey(x => new { x.TemplateId, x.Id }).HasConstraintName("fk_shop_template_revision").OnDelete(DeleteBehavior.Restrict);
        shop.HasOne<ShopMediaAsset>().WithMany().HasForeignKey(x => new { x.LogoAssetId, x.OwnerUserId })
            .HasPrincipalKey(x => new { x.Id, x.OwnerUserId }).HasConstraintName("fk_shop_logo_owner").OnDelete(DeleteBehavior.Restrict);
        shop.HasOne<ShopMediaAsset>().WithMany().HasForeignKey(x => new { x.BannerAssetId, x.OwnerUserId })
            .HasPrincipalKey(x => new { x.Id, x.OwnerUserId }).HasConstraintName("fk_shop_banner_owner").OnDelete(DeleteBehavior.Restrict);
        shop.HasOne<User>().WithMany().HasForeignKey(x => x.SuspendedByUserId).OnDelete(DeleteBehavior.Restrict);
        shop.HasIndex(x => new { x.Status, x.CreatedAt, x.Id }).HasFilter("is_deleted = false");
        shop.ToTable("shops", table => {
            table.HasCheckConstraint("ck_shop_version", "version > 0");
            table.HasCheckConstraint("ck_shop_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$' AND length(slug) BETWEEN 3 AND 80");
            table.HasCheckConstraint("ck_shop_colors", "jsonb_typeof(custom_colors) = 'object'");
            table.HasCheckConstraint("ck_shop_suspension", "(status = 'Suspended' AND pre_suspension_status IN ('Active','Inactive') AND suspended_at_utc IS NOT NULL AND suspended_by_user_id IS NOT NULL AND length(suspension_reason) >= 10) OR (status <> 'Suspended' AND pre_suspension_status IS NULL AND suspended_at_utc IS NULL AND suspended_by_user_id IS NULL AND suspension_reason IS NULL)");
        });

        var template = model.Entity<ShopTemplate>(); Names(template, "shop_templates");
        template.Property(x => x.Code).HasMaxLength(50);
        template.Property(x => x.Version).HasDefaultValue(1L).IsConcurrencyToken();
        template.HasIndex(x => x.Code).IsUnique().HasDatabaseName("shop_templates_code_key");
        template.HasOne<ShopTemplateRevision>().WithMany().HasForeignKey(x => new { x.Id, x.CurrentRevisionId })
            .HasPrincipalKey(x => new { x.TemplateId, x.Id }).HasConstraintName("fk_template_current_revision").OnDelete(DeleteBehavior.Restrict);
        template.HasOne<ShopMediaAsset>().WithMany().HasForeignKey(x => x.PreviewAssetId).OnDelete(DeleteBehavior.Restrict);
        template.HasIndex(x => new { x.IsActive, x.SortOrder, x.Code, x.Id }).HasFilter("is_deleted = false");
        template.ToTable("shop_templates", t => {
            t.HasCheckConstraint("ck_template_version", "version > 0");
            t.HasCheckConstraint("ck_template_current", "NOT is_active OR current_revision_id IS NOT NULL");
        });

        var revision = model.Entity<ShopTemplateRevision>(); Names(revision, "shop_template_revisions");
        revision.HasKey(x => x.Id);
        revision.HasAlternateKey(x => new { x.TemplateId, x.Id }).HasName("uq_shop_revision_template");
        revision.HasIndex(x => new { x.TemplateId, x.Revision }).IsUnique();
        revision.HasOne<ShopTemplate>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        revision.Property(x => x.RendererKey).HasMaxLength(50);
        revision.Property(x => x.CssVariables).HasColumnType("jsonb");
        revision.Property(x => x.LayoutConfig).HasColumnType("jsonb");
        revision.ToTable("shop_template_revisions", t => {
            t.HasCheckConstraint("ck_shop_revision", "revision > 0 AND schema_version = 1 AND jsonb_typeof(css_variables) = 'object' AND jsonb_typeof(layout_config) = 'object'");
        });

        var asset = model.Entity<ShopMediaAsset>(); Names(asset, "shop_media_assets");
        asset.HasKey(x => x.Id);
        asset.HasAlternateKey(x => new { x.Id, x.OwnerUserId }).HasName("uq_shop_media_owner");
        asset.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        asset.HasIndex(x => new { x.OwnerUserId, x.UploadOperationId }).IsUnique();
        asset.HasIndex(x => new { x.OwnerUserId, x.CreatedAt });
        asset.Property(x => x.Purpose).HasMaxLength(30);
        asset.Property(x => x.RequestHash).HasMaxLength(64);
        asset.Property(x => x.Sha256).HasMaxLength(64);
        asset.Property(x => x.Status).HasMaxLength(30);
        asset.Property(x => x.ContentType).HasMaxLength(100);
        asset.ToTable("shop_media_assets", t => {
            t.HasCheckConstraint("ck_shop_media_purpose", "purpose IN ('Logo','Banner','TemplatePreview','ProductGallery','VariantImage')");
            t.HasCheckConstraint("ck_shop_media_status", "status IN ('Uploading','Ready','Failed','Quarantined','Deleting','Deleted')");
        });

        var replay = model.Entity<ShopIdempotencyRecord>(); Names(replay, "shop_idempotency_records");
        replay.HasKey(x => x.Id);
        replay.HasIndex(x => new { x.ActorUserId, x.Operation, x.ResourceId, x.Key }).IsUnique();
        replay.HasIndex(x => x.ExpiresAtUtc);
        replay.Property(x => x.Operation).HasMaxLength(100);
        replay.Property(x => x.RequestHash).HasMaxLength(64);
        replay.Property(x => x.ResponseJson).HasColumnType("jsonb");

        var catalog = model.Entity<ShopTemplateCatalogState>(); Names(catalog, "shop_template_catalog_state");
        catalog.HasKey(x => x.Id);
        catalog.Property(x => x.Id).ValueGeneratedNever();
        catalog.HasOne<ShopTemplate>().WithMany().HasForeignKey(x => x.DefaultTemplateId).OnDelete(DeleteBehavior.Restrict);
        catalog.ToTable("shop_template_catalog_state", t => t.HasCheckConstraint("ck_shop_catalog_singleton", "id = 1 AND version > 0"));
    }

    private static void Names<T>(EntityTypeBuilder<T> entity, string table) where T : class
    {
        entity.ToTable(table);
        foreach (var property in entity.Metadata.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
