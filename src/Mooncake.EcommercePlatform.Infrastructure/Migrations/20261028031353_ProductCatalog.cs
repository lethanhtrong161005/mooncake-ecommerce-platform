using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Mooncake.EcommercePlatform.Domain.Enums;
using NpgsqlTypes;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_media_purpose",
                table: "shop_media_assets");

            // Legacy deployments may have this uniqueness rule as either the original
            // EF-created index or a database constraint with the same name.
            migrationBuilder.Sql("""
                ALTER TABLE product_variants DROP CONSTRAINT IF EXISTS product_variants_sku_key;
                DROP INDEX IF EXISTS product_variants_sku_key;
                """);

            migrationBuilder.AddColumn<string>(
                name: "region_code",
                table: "shops",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at_utc",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "products",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "crust_type",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_asset_id",
                table: "product_variants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "inventory_version",
                table: "product_variants",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<bool>(
                name: "is_gift_set",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "normalized_sku",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "shop_id",
                table: "product_variants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "media_asset_id",
                table: "product_images",
                type: "uuid",
                nullable: true);

            // Backfill catalog columns before introducing scoped keys and constraints.  Existing
            // installations may have product rows created before the catalog aggregate existed.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM products p
                        LEFT JOIN shops s ON s.id = p.shop_id
                        LEFT JOIN categories c ON c.id = p.category_id
                        WHERE s.id IS NULL OR c.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Product catalog migration stopped: every existing product must reference an existing shop and category.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM product_variants v
                        LEFT JOIN products p ON p.id = v.product_id
                        WHERE p.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Product catalog migration stopped: every existing variant must reference an existing product.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM product_variants v
                        JOIN products p ON p.id = v.product_id
                        WHERE NOT v.is_deleted
                        GROUP BY p.shop_id, upper(trim(v.sku))
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Product catalog migration stopped: active variants must have unique SKU values within a shop.';
                    END IF;
                END $$;

                UPDATE products
                SET normalized_name = upper(trim(name)),
                    is_published = status IN ('Active', 'OutOfStock'),
                    published_at_utc = CASE
                        WHEN status IN ('Active', 'OutOfStock') THEN COALESCE(updated_at, created_at)
                        ELSE NULL
                    END,
                    version = GREATEST(version, 1);

                UPDATE product_variants v
                SET shop_id = p.shop_id,
                    normalized_sku = upper(trim(v.sku)),
                    inventory_version = GREATEST(v.inventory_version, 1)
                FROM products p
                WHERE p.id = v.product_id;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "uq_product_shop",
                table: "products",
                columns: new[] { "id", "shop_id" });

            migrationBuilder.CreateTable(
                name: "product_catalog_outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_catalog_outbox_events", x => x.id);
                    table.CheckConstraint("ck_product_outbox_attempt", "attempt_count >= 0");
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    delta = table.Column<int>(type: "integer", nullable: false),
                    previous_qty = table.Column<int>(type: "integer", nullable: false),
                    current_qty = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_product_inventory_movement", "delta <> 0 AND previous_qty >= 0 AND current_qty >= 0 AND current_qty = previous_qty + delta");
                    table.ForeignKey(
                        name: "FK_product_inventory_movements_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_inventory_movements_products_product_id_shop_id",
                        columns: x => new { x.product_id, x.shop_id },
                        principalTable: "products",
                        principalColumns: new[] { "id", "shop_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_inventory_movements_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_search_documents",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    shop_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    shop_slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    category_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    search_text = table.Column<string>(type: "text", nullable: false),
                    primary_image_url = table.Column<string>(type: "text", nullable: true),
                    min_price = table.Column<decimal>(type: "numeric", nullable: false),
                    max_price = table.Column<decimal>(type: "numeric", nullable: false),
                    min_order_qty = table.Column<int>(type: "integer", nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supports_custom_packaging = table.Column<bool>(type: "boolean", nullable: false),
                    effective_status = table.Column<ProductStatus>(type: "product_status", nullable: false),
                    reputation_score = table.Column<decimal>(type: "numeric", nullable: false),
                    average_rating = table.Column<decimal>(type: "numeric", nullable: true),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    sold_count = table.Column<long>(type: "bigint", nullable: false),
                    region_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    projected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', coalesce(search_text, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_search_documents", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "product_view_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    viewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_view_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_view_events_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_view_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Request a projection rebuild for pre-existing products.  uuid_generate_v4 is
            // provided by the project's existing uuid-ossp extension.
            migrationBuilder.Sql("""
                INSERT INTO product_catalog_outbox_events
                    (id, product_id, source_version, event_type, attempt_count, available_at_utc,
                     processed_at_utc, last_error_code, is_deleted, created_at, updated_at, created_by, updated_by)
                SELECT uuid_generate_v4(), p.id, p.version, 'ProductBackfillRequested', 0, now(),
                       NULL, NULL, false, now(), now(), NULL, NULL
                FROM products p
                WHERE NOT p.is_deleted;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_media_purpose",
                table: "shop_media_assets",
                sql: "purpose IN ('Logo','Banner','TemplatePreview','ProductGallery','VariantImage')");

            migrationBuilder.CreateIndex(
                name: "IX_products_is_published_category_id_created_at_id",
                table: "products",
                columns: new[] { "is_published", "category_id", "created_at", "id" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_products_shop_id_updated_at_id",
                table: "products",
                columns: new[] { "shop_id", "updated_at", "id" },
                filter: "is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_price",
                table: "products",
                sql: "base_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_quantity",
                table: "products",
                sql: "min_order_qty > 0 AND (max_order_qty IS NULL OR max_order_qty >= min_order_qty)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_version",
                table: "products",
                sql: "version > 0");

            migrationBuilder.CreateIndex(
                name: "idx_product_variants_sku",
                table: "product_variants",
                column: "sku");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_image_asset_id",
                table: "product_variants",
                column: "image_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_product_id_shop_id",
                table: "product_variants",
                columns: new[] { "product_id", "shop_id" });

            migrationBuilder.CreateIndex(
                name: "uq_product_variant_shop_sku",
                table: "product_variants",
                columns: new[] { "shop_id", "normalized_sku" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variant_inventory",
                table: "product_variants",
                sql: "stock_qty >= 0 AND inventory_version > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variant_weight",
                table: "product_variants",
                sql: "weight_gram IS NULL OR weight_gram > 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_media_asset_id",
                table: "product_images",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_image_order",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "uq_product_media_asset",
                table: "product_images",
                columns: new[] { "product_id", "media_asset_id" },
                unique: true,
                filter: "is_deleted = false AND media_asset_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_primary_image",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "is_deleted = false AND is_primary = true");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_image_sort",
                table: "product_images",
                sql: "sort_order BETWEEN 0 AND 9");

            migrationBuilder.CreateIndex(
                name: "IX_product_catalog_outbox_events_processed_at_utc_available_at~",
                table: "product_catalog_outbox_events",
                columns: new[] { "processed_at_utc", "available_at_utc", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_movements_actor_user_id",
                table: "product_inventory_movements",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_movements_product_id_shop_id",
                table: "product_inventory_movements",
                columns: new[] { "product_id", "shop_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_movements_source_type_source_id_variant_id",
                table: "product_inventory_movements",
                columns: new[] { "source_type", "source_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_inventory_movements_variant_id_created_at",
                table: "product_inventory_movements",
                columns: new[] { "variant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_product_search_vector",
                table: "product_search_documents",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_product_search_documents_category_id_effective_status_creat~",
                table: "product_search_documents",
                columns: new[] { "category_id", "effective_status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_product_search_documents_min_price",
                table: "product_search_documents",
                column: "min_price");

            migrationBuilder.CreateIndex(
                name: "IX_product_search_documents_region_code_reputation_score",
                table: "product_search_documents",
                columns: new[] { "region_code", "reputation_score" });

            migrationBuilder.CreateIndex(
                name: "IX_product_view_events_product_id_viewed_at_utc",
                table: "product_view_events",
                columns: new[] { "product_id", "viewed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_product_view_events_user_id_viewed_at_utc",
                table: "product_view_events",
                columns: new[] { "user_id", "viewed_at_utc" });

            migrationBuilder.AddForeignKey(
                name: "FK_product_images_products_product_id",
                table: "product_images",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_images_shop_media_assets_media_asset_id",
                table: "product_images",
                column: "media_asset_id",
                principalTable: "shop_media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_variants_products_product_id_shop_id",
                table: "product_variants",
                columns: new[] { "product_id", "shop_id" },
                principalTable: "products",
                principalColumns: new[] { "id", "shop_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_product_variants_shop_media_assets_image_asset_id",
                table: "product_variants",
                column: "image_asset_id",
                principalTable: "shop_media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_shops_shop_id",
                table: "products",
                column: "shop_id",
                principalTable: "shops",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_images_products_product_id",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "FK_product_images_shop_media_assets_media_asset_id",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "FK_product_variants_products_product_id_shop_id",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "FK_product_variants_shop_media_assets_image_asset_id",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_category_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_shops_shop_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "product_catalog_outbox_events");

            migrationBuilder.DropTable(
                name: "product_inventory_movements");

            migrationBuilder.DropTable(
                name: "product_search_documents");

            migrationBuilder.DropTable(
                name: "product_view_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_media_purpose",
                table: "shop_media_assets");

            migrationBuilder.DropUniqueConstraint(
                name: "uq_product_shop",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_is_published_category_id_created_at_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_shop_id_updated_at_id",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_price",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_quantity",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_version",
                table: "products");

            migrationBuilder.DropIndex(
                name: "idx_product_variants_sku",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_image_asset_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_variants_product_id_shop_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "uq_product_variant_shop_sku",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variant_inventory",
                table: "product_variants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variant_weight",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "IX_product_images_media_asset_id",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "uq_product_image_order",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "uq_product_media_asset",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "uq_product_primary_image",
                table: "product_images");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_image_sort",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "region_code",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "is_published",
                table: "products");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_at_utc",
                table: "products");

            migrationBuilder.DropColumn(
                name: "version",
                table: "products");

            migrationBuilder.DropColumn(
                name: "crust_type",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "image_asset_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "inventory_version",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_gift_set",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "normalized_sku",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "shop_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "media_asset_id",
                table: "product_images");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_media_purpose",
                table: "shop_media_assets",
                sql: "purpose IN ('Logo','Banner','TemplatePreview')");

            migrationBuilder.CreateIndex(
                name: "product_variants_sku_key",
                table: "product_variants",
                column: "sku",
                unique: true);
        }
    }
}
