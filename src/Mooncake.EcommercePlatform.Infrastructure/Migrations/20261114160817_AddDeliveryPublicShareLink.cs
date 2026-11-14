using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPublicShareLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "share_token",
                table: "deliveries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "share_token_created_at_utc",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "share_token_expires_at_utc",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "share_token_revoked_at_utc",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_del_share_token",
                table: "deliveries",
                column: "share_token",
                unique: true,
                filter: "(share_token IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_del_share_token",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "share_token",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "share_token_created_at_utc",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "share_token_expires_at_utc",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "share_token_revoked_at_utc",
                table: "deliveries");
        }
    }
}
