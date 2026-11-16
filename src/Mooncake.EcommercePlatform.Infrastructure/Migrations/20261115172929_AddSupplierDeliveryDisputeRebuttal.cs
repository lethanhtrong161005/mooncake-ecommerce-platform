using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierDeliveryDisputeRebuttal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "rebuttal_deadline_at",
                table: "order_delivery_disputes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_rebuttal_at",
                table: "order_delivery_disputes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_rebuttal_evidence_urls_json",
                table: "order_delivery_disputes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_rebuttal_notes",
                table: "order_delivery_disputes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rebuttal_deadline_at",
                table: "order_delivery_disputes");

            migrationBuilder.DropColumn(
                name: "supplier_rebuttal_at",
                table: "order_delivery_disputes");

            migrationBuilder.DropColumn(
                name: "supplier_rebuttal_evidence_urls_json",
                table: "order_delivery_disputes");

            migrationBuilder.DropColumn(
                name: "supplier_rebuttal_notes",
                table: "order_delivery_disputes");
        }
    }
}
