using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRefundFieldsAndSingleOpenPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                table: "payments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "payments",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "payments_one_open_payment_per_milestone_key",
                table: "payments",
                column: "milestone_id",
                unique: true,
                filter: "(milestone_id IS NOT NULL AND status IN ('Pending', 'Processing', 'Completed') AND is_deleted = false)");

            migrationBuilder.CreateIndex(
                name: "payments_one_open_payment_per_order_key",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "(order_id IS NOT NULL AND status IN ('Pending', 'Processing', 'Completed') AND is_deleted = false)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "payments_one_open_payment_per_milestone_key",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "payments_one_open_payment_per_order_key",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refund_reason",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "payments");
        }
    }
}
