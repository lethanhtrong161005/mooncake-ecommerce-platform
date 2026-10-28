using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Mooncake.EcommercePlatform.Domain.Enums;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShopManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ShopManagementSql.Preflight);
            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "shops",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "shops",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "shops",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "custom_colors",
                table: "shops",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "banner_asset_id",
                table: "shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "logo_asset_id",
                table: "shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "shops",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<ShopStatus>(
                name: "pre_suspension_status",
                table: "shops",
                type: "shop_status",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_address",
                table: "shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_contact_email",
                table: "shops",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "public_contact_phone",
                table: "shops",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at_utc",
                table: "shops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "suspended_by_user_id",
                table: "shops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "shops",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_revision_id",
                table: "shops",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "shops",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "shop_templates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "current_revision_id",
                table: "shop_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_premium",
                table: "shop_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "preview_asset_id",
                table: "shop_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "shop_templates",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);



            migrationBuilder.CreateTable(
                name: "shop_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false),
                    http_status = table.Column<int>(type: "integer", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_idempotency_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shop_media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    upload_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    lease_token = table.Column<Guid>(type: "uuid", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    provider_public_id = table.Column<string>(type: "text", nullable: true),
                    public_url = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    byte_count = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_media_assets", x => x.id);
                    table.UniqueConstraint("uq_shop_media_owner", x => new { x.id, x.owner_user_id });
                    table.CheckConstraint("ck_shop_media_purpose", "purpose IN ('Logo','Banner','TemplatePreview')");
                    table.CheckConstraint("ck_shop_media_status", "status IN ('Uploading','Ready','Failed','Quarantined','Deleting','Deleted')");
                    table.ForeignKey(
                        name: "FK_shop_media_assets_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shop_template_catalog_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    default_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_template_catalog_state", x => x.id);
                    table.CheckConstraint("ck_shop_catalog_singleton", "id = 1 AND version > 0");
                    table.ForeignKey(
                        name: "FK_shop_template_catalog_state_shop_templates_default_template~",
                        column: x => x.default_template_id,
                        principalTable: "shop_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shop_template_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    renderer_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    css_variables = table.Column<string>(type: "jsonb", nullable: false),
                    layout_config = table.Column<string>(type: "jsonb", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_template_revisions", x => x.id);
                    table.UniqueConstraint("uq_shop_revision_template", x => new { x.template_id, x.id });
                    table.CheckConstraint("ck_shop_revision", "revision > 0 AND schema_version = 1 AND jsonb_typeof(css_variables) = 'object' AND jsonb_typeof(layout_config) = 'object'");
                    table.ForeignKey(
                        name: "FK_shop_template_revisions_shop_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "shop_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(ShopManagementSql.Backfill);

            migrationBuilder.CreateIndex(
                name: "IX_shops_banner_asset_id_owner_user_id",
                table: "shops",
                columns: new[] { "banner_asset_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shops_logo_asset_id_owner_user_id",
                table: "shops",
                columns: new[] { "logo_asset_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shops_status_created_at_id",
                table: "shops",
                columns: new[] { "status", "created_at", "id" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_shops_supplier_id_owner_user_id",
                table: "shops",
                columns: new[] { "supplier_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shops_suspended_by_user_id",
                table: "shops",
                column: "suspended_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shops_template_id_template_revision_id",
                table: "shops",
                columns: new[] { "template_id", "template_revision_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_colors",
                table: "shops",
                sql: "jsonb_typeof(custom_colors) = 'object'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_slug",
                table: "shops",
                sql: "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$' AND length(slug) BETWEEN 3 AND 80");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops",
                sql: "(status = 'Suspended' AND pre_suspension_status IN ('Active','Inactive') AND suspended_at_utc IS NOT NULL AND suspended_by_user_id IS NOT NULL AND length(suspension_reason) >= 10) OR (status <> 'Suspended' AND pre_suspension_status IS NULL AND suspended_at_utc IS NULL AND suspended_by_user_id IS NULL AND suspension_reason IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shop_version",
                table: "shops",
                sql: "version > 0");

            migrationBuilder.CreateIndex(
                name: "IX_shop_templates_id_current_revision_id",
                table: "shop_templates",
                columns: new[] { "id", "current_revision_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shop_templates_is_active_sort_order_code_id",
                table: "shop_templates",
                columns: new[] { "is_active", "sort_order", "code", "id" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_shop_templates_preview_asset_id",
                table: "shop_templates",
                column: "preview_asset_id");

            migrationBuilder.CreateIndex(
                name: "shop_templates_code_key",
                table: "shop_templates",
                column: "code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_template_current",
                table: "shop_templates",
                sql: "NOT is_active OR current_revision_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_template_version",
                table: "shop_templates",
                sql: "version > 0");

            migrationBuilder.CreateIndex(
                name: "IX_shop_idempotency_records_actor_user_id_operation_resource_i~",
                table: "shop_idempotency_records",
                columns: new[] { "actor_user_id", "operation", "resource_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shop_idempotency_records_expires_at_utc",
                table: "shop_idempotency_records",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_shop_media_assets_owner_user_id_created_at",
                table: "shop_media_assets",
                columns: new[] { "owner_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shop_media_assets_owner_user_id_upload_operation_id",
                table: "shop_media_assets",
                columns: new[] { "owner_user_id", "upload_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shop_template_catalog_state_default_template_id",
                table: "shop_template_catalog_state",
                column: "default_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_shop_template_revisions_template_id_revision",
                table: "shop_template_revisions",
                columns: new[] { "template_id", "revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_shop_templates_shop_media_assets_preview_asset_id",
                table: "shop_templates",
                column: "preview_asset_id",
                principalTable: "shop_media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_template_current_revision",
                table: "shop_templates",
                columns: new[] { "id", "current_revision_id" },
                principalTable: "shop_template_revisions",
                principalColumns: new[] { "template_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shops_users_suspended_by_user_id",
                table: "shops",
                column: "suspended_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shop_banner_owner",
                table: "shops",
                columns: new[] { "banner_asset_id", "owner_user_id" },
                principalTable: "shop_media_assets",
                principalColumns: new[] { "id", "owner_user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shop_logo_owner",
                table: "shops",
                columns: new[] { "logo_asset_id", "owner_user_id" },
                principalTable: "shop_media_assets",
                principalColumns: new[] { "id", "owner_user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shop_supplier_owner",
                table: "shops",
                columns: new[] { "supplier_id", "owner_user_id" },
                principalTable: "supplier_profiles",
                principalColumns: new[] { "id", "user_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shop_template_revision",
                table: "shops",
                columns: new[] { "template_id", "template_revision_id" },
                principalTable: "shop_template_revisions",
                principalColumns: new[] { "template_id", "id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("ALTER TABLE shop_templates ALTER CONSTRAINT fk_template_current_revision DEFERRABLE INITIALLY DEFERRED;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shop_templates_shop_media_assets_preview_asset_id",
                table: "shop_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_template_current_revision",
                table: "shop_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_shops_users_suspended_by_user_id",
                table: "shops");

            migrationBuilder.DropForeignKey(
                name: "fk_shop_banner_owner",
                table: "shops");

            migrationBuilder.DropForeignKey(
                name: "fk_shop_logo_owner",
                table: "shops");

            migrationBuilder.DropForeignKey(
                name: "fk_shop_supplier_owner",
                table: "shops");

            migrationBuilder.DropForeignKey(
                name: "fk_shop_template_revision",
                table: "shops");

            migrationBuilder.DropTable(
                name: "shop_idempotency_records");

            migrationBuilder.DropTable(
                name: "shop_media_assets");

            migrationBuilder.DropTable(
                name: "shop_template_catalog_state");

            migrationBuilder.DropTable(
                name: "shop_template_revisions");



            migrationBuilder.DropIndex(
                name: "IX_shops_banner_asset_id_owner_user_id",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shops_logo_asset_id_owner_user_id",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shops_status_created_at_id",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shops_supplier_id_owner_user_id",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shops_suspended_by_user_id",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shops_template_id_template_revision_id",
                table: "shops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_colors",
                table: "shops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_slug",
                table: "shops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_suspension",
                table: "shops");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shop_version",
                table: "shops");

            migrationBuilder.DropIndex(
                name: "IX_shop_templates_id_current_revision_id",
                table: "shop_templates");

            migrationBuilder.DropIndex(
                name: "IX_shop_templates_is_active_sort_order_code_id",
                table: "shop_templates");

            migrationBuilder.DropIndex(
                name: "IX_shop_templates_preview_asset_id",
                table: "shop_templates");

            migrationBuilder.DropIndex(
                name: "shop_templates_code_key",
                table: "shop_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_template_current",
                table: "shop_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_template_version",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "banner_asset_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "logo_asset_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "pre_suspension_status",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "public_address",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "public_contact_email",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "public_contact_phone",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "suspended_at_utc",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "suspended_by_user_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "template_revision_id",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "version",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "code",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "current_revision_id",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "is_premium",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "preview_asset_id",
                table: "shop_templates");

            migrationBuilder.DropColumn(
                name: "version",
                table: "shop_templates");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "shops",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "shops",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "shops",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "custom_colors",
                table: "shops",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");
        }
    }
}
