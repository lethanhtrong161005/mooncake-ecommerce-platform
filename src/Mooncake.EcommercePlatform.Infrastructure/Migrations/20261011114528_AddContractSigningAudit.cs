using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSigningAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "contract_signing_code_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "contract_signing_code_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_signing_code_hash",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_signature_ip",
                table: "contracts",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_signature_user_agent",
                table: "contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_signature_ip",
                table: "contracts",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_signature_user_agent",
                table: "contracts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contract_signing_code_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "contract_signing_code_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "contract_signing_code_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "customer_signature_ip",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "customer_signature_user_agent",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "supplier_signature_ip",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "supplier_signature_user_agent",
                table: "contracts");
        }
    }
}
