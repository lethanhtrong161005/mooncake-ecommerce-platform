using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_anonymous",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden_by_admin",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "media_urls",
                table: "reviews",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_replied_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_reply",
                table: "reviews",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "reviews_contract_id_key",
                table: "reviews",
                column: "contract_id",
                unique: true,
                filter: "((contract_id IS NOT NULL) AND (is_deleted = false))");

            migrationBuilder.CreateIndex(
                name: "reviews_order_id_key",
                table: "reviews",
                column: "order_id",
                unique: true,
                filter: "((order_id IS NOT NULL) AND (is_deleted = false))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "reviews_contract_id_key",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "reviews_order_id_key",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_anonymous",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_hidden_by_admin",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "media_urls",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "supplier_replied_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "supplier_reply",
                table: "reviews");
        }
    }
}
