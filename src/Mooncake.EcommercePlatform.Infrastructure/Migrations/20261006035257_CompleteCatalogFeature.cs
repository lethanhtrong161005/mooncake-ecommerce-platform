using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCatalogFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "product_variants_sku_key",
                table: "product_variants");

            migrationBuilder.AddColumn<bool>(
                name: "is_premium",
                table: "shop_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "product_variants_sku_key",
                table: "product_variants",
                column: "sku",
                unique: true,
                filter: "(is_deleted = false)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "product_variants_sku_key",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_premium",
                table: "shop_templates");

            migrationBuilder.CreateIndex(
                name: "product_variants_sku_key",
                table: "product_variants",
                column: "sku",
                unique: true);
        }
    }
}
