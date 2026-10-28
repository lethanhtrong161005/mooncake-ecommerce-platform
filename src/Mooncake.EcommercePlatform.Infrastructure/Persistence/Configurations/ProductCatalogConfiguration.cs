namespace Mooncake.EcommercePlatform.Infrastructure.Persistence.Configurations;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mooncake.EcommercePlatform.Domain.Entities;
using NpgsqlTypes;

/// <summary>Catalog aggregate integrity, inventory ledger and rebuildable search projections.</summary>
public static class ProductCatalogConfiguration
{
    public static void ConfigureCatalog(this ModelBuilder model)
    {
        var product = model.Entity<Product>();
        product.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();
        product.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(255).HasDefaultValue(string.Empty);
        product.Property(x => x.IsPublished).HasColumnName("is_published");
        product.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        product.HasAlternateKey(x => new { x.Id, x.ShopId }).HasName("uq_product_shop");
        product.HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        product.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        product.HasIndex(x => new { x.ShopId, x.UpdatedAt, x.Id }).HasFilter("is_deleted = false");
        product.HasIndex(x => new { x.IsPublished, x.CategoryId, x.CreatedAt, x.Id }).HasFilter("is_deleted = false");
        product.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_product_version", "version > 0");
            table.HasCheckConstraint("ck_product_price", "base_price >= 0");
            table.HasCheckConstraint("ck_product_quantity", "min_order_qty > 0 AND (max_order_qty IS NULL OR max_order_qty >= min_order_qty)");
        });

        var variant = model.Entity<ProductVariant>();
        variant.Property(x => x.ShopId).HasColumnName("shop_id");
        variant.Property(x => x.NormalizedSku).HasColumnName("normalized_sku").HasMaxLength(100).HasDefaultValue(string.Empty);
        variant.Property(x => x.CrustType).HasColumnName("crust_type").HasMaxLength(100);
        variant.Property(x => x.IsGiftSet).HasColumnName("is_gift_set");
        variant.Property(x => x.InventoryVersion).HasColumnName("inventory_version").HasDefaultValue(1L).IsConcurrencyToken();
        variant.Property(x => x.ImageAssetId).HasColumnName("image_asset_id");
        variant.HasOne<Product>().WithMany().HasForeignKey(x => new { x.ProductId, x.ShopId })
            .HasPrincipalKey(x => new { x.Id, x.ShopId }).OnDelete(DeleteBehavior.Restrict);
        variant.HasOne<ShopMediaAsset>().WithMany().HasForeignKey(x => x.ImageAssetId).OnDelete(DeleteBehavior.Restrict);
        variant.HasIndex(x => new { x.ShopId, x.NormalizedSku }).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_product_variant_shop_sku");
        variant.ToTable("product_variants", table =>
        {
            table.HasCheckConstraint("ck_product_variant_inventory", "stock_qty >= 0 AND inventory_version > 0");
            table.HasCheckConstraint("ck_product_variant_weight", "weight_gram IS NULL OR weight_gram > 0");
        });

        var image = model.Entity<ProductImage>();
        image.Property(x => x.MediaAssetId).HasColumnName("media_asset_id");
        image.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        image.HasOne<ShopMediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
        image.HasIndex(x => x.ProductId).IsUnique().HasFilter("is_deleted = false AND is_primary = true")
            .HasDatabaseName("uq_product_primary_image");
        image.HasIndex(x => new { x.ProductId, x.MediaAssetId }).IsUnique().HasFilter("is_deleted = false AND media_asset_id IS NOT NULL")
            .HasDatabaseName("uq_product_media_asset");
        image.HasIndex(x => new { x.ProductId, x.SortOrder }).IsUnique().HasFilter("is_deleted = false")
            .HasDatabaseName("uq_product_image_order");
        image.ToTable("product_images", table => table.HasCheckConstraint("ck_product_image_sort", "sort_order BETWEEN 0 AND 9"));

        var movement = model.Entity<ProductInventoryMovement>(); Names(movement, "product_inventory_movements");
        movement.HasKey(x => x.Id);
        movement.HasOne<Product>().WithMany().HasForeignKey(x => new { x.ProductId, x.ShopId })
            .HasPrincipalKey(x => new { x.Id, x.ShopId }).OnDelete(DeleteBehavior.Restrict);
        movement.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        movement.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        movement.Property(x => x.SourceType).HasMaxLength(40);
        movement.Property(x => x.SourceId).HasMaxLength(128);
        movement.Property(x => x.Reason).HasMaxLength(500);
        movement.HasIndex(x => new { x.SourceType, x.SourceId, x.VariantId }).IsUnique();
        movement.HasIndex(x => new { x.VariantId, x.CreatedAt });
        movement.ToTable("product_inventory_movements", table =>
            table.HasCheckConstraint("ck_product_inventory_movement", "delta <> 0 AND previous_qty >= 0 AND current_qty >= 0 AND current_qty = previous_qty + delta"));

        var outbox = model.Entity<ProductCatalogOutboxEvent>(); Names(outbox, "product_catalog_outbox_events");
        outbox.HasKey(x => x.Id);
        outbox.HasIndex(x => new { x.ProcessedAtUtc, x.AvailableAtUtc, x.CreatedAt });
        outbox.Property(x => x.EventType).HasMaxLength(80);
        outbox.Property(x => x.LastErrorCode).HasMaxLength(100);
        outbox.ToTable("product_catalog_outbox_events", table => table.HasCheckConstraint("ck_product_outbox_attempt", "attempt_count >= 0"));

        var search = model.Entity<ProductSearchDocument>(); Names(search, "product_search_documents");
        search.HasKey(x => x.ProductId);
        search.Property(x => x.Name).HasMaxLength(255);
        search.Property(x => x.ShopName).HasMaxLength(255);
        search.Property(x => x.ShopSlug).HasMaxLength(80);
        search.Property(x => x.CategoryName).HasMaxLength(100);
        search.Property(x => x.Unit).HasMaxLength(50);
        search.Property(x => x.RegionCode).HasMaxLength(20);
        search.Property<NpgsqlTsVector>("SearchVector").HasColumnName("search_vector")
            .HasComputedColumnSql("to_tsvector('simple', coalesce(search_text, ''))", stored: true);
        search.HasIndex("SearchVector").HasMethod("GIN").HasDatabaseName("idx_product_search_vector");
        search.HasIndex(x => new { x.CategoryId, x.EffectiveStatus, x.CreatedAtUtc });
        search.HasIndex(x => new { x.RegionCode, x.ReputationScore });
        search.HasIndex(x => x.MinPrice);

        var view = model.Entity<ProductViewEvent>(); Names(view, "product_view_events");
        view.HasKey(x => x.Id);
        view.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        view.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        view.Property(x => x.Source).HasMaxLength(30);
        view.HasIndex(x => new { x.ProductId, x.ViewedAtUtc });
        view.HasIndex(x => new { x.UserId, x.ViewedAtUtc });
    }

    private static void Names<T>(EntityTypeBuilder<T> entity, string table) where T : class
    {
        entity.ToTable(table);
        foreach (var property in entity.Metadata.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
