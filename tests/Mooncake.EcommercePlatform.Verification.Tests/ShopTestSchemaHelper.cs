namespace Mooncake.EcommercePlatform.Verification.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Mooncake.EcommercePlatform.Domain.Entities;

/// <summary>Reconstructs the pre-shop runtime schema for real migration integration tests.</summary>
public static class ShopTestSchemaHelper
{
    public static void RemoveExpansion(ModelBuilder model)
    {
        foreach (var index in model.Entity<Product>().Metadata.GetIndexes().Where(x => x.GetDatabaseName() == "idx_shop_public_product_feed").ToArray())
            model.Entity<Product>().Metadata.RemoveIndex(index);
        foreach (var entity in model.Model.GetEntityTypes().ToArray())
            foreach (var foreign in entity.GetForeignKeys().ToArray())
                if (entity.ClrType == typeof(Shop) || entity.ClrType == typeof(ShopTemplate) ||
                    foreign.PrincipalEntityType.ClrType == typeof(ShopMediaAsset) || foreign.PrincipalEntityType.ClrType == typeof(ShopTemplateRevision))
                    entity.RemoveForeignKey(foreign);
        model.Ignore<ShopTemplateRevision>(); model.Ignore<ShopTemplateCatalogState>();
        model.Ignore<ShopMediaAsset>(); model.Ignore<ShopIdempotencyRecord>();
        foreach (var entity in new[] { model.Entity<Shop>().Metadata, model.Entity<ShopTemplate>().Metadata })
            foreach (var check in entity.GetCheckConstraints().ToArray()) entity.RemoveCheckConstraint(check.Name!);
        var shop = model.Entity<Shop>();
        foreach (var index in shop.Metadata.GetIndexes().Where(x => x.GetDatabaseName() == "IX_shops_status_created_at_id").ToArray())
            shop.Metadata.RemoveIndex(index);
        foreach (var name in new[] { "OwnerUserId", "Version", "TemplateRevisionId", "LogoAssetId", "BannerAssetId",
            "PublicContactEmail", "PublicContactPhone", "PublicAddress", "PreSuspensionStatus", "SuspendedAtUtc", "SuspendedByUserId", "SuspensionReason" }) shop.Ignore(name);
        shop.Property(x => x.Name).IsRequired(false).HasMaxLength(255);
        shop.Property(x => x.Slug).HasMaxLength(255);
        shop.Property(x => x.Description).HasColumnType("text").Metadata.SetMaxLength(null);
        shop.Property(x => x.CustomColors).IsRequired(false).HasDefaultValue((string?)null);
        var template = model.Entity<ShopTemplate>();
        foreach (var name in new[] { "Code", "Version", "CurrentRevisionId", "PreviewAssetId", "IsPremium" }) template.Ignore(name);
        foreach (var key in model.Entity<SupplierProfile>().Metadata.GetKeys().Where(x => !x.IsPrimaryKey()).ToArray())
            model.Entity<SupplierProfile>().Metadata.RemoveKey(key);
    }
}
