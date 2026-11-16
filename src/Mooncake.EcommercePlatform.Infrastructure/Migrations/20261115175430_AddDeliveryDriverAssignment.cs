using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryDriverAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "driver_claimed_at",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_name",
                table: "deliveries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "driver_phone",
                table: "deliveries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "share_code",
                table: "deliveries",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_plate",
                table: "deliveries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_del_share_code",
                table: "deliveries",
                column: "share_code",
                unique: true,
                filter: "(share_code IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_del_share_code",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "driver_claimed_at",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "driver_name",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "driver_phone",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "share_code",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "vehicle_plate",
                table: "deliveries");
        }
    }
}
