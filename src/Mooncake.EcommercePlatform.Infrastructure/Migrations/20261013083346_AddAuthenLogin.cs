namespace Mooncake.EcommercePlatform.Infrastructure.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Mooncake.EcommercePlatform.Infrastructure.Persistence;

/// <summary>Adds refresh sessions and enforces unique active phone numbers.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261013083346_AddAuthenLogin")]
public partial class AddAuthenLogin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE users
            SET phone = CASE
                WHEN left(clean_phone, 3) = '+84' THEN '0' || substring(clean_phone from 4)
                WHEN left(clean_phone, 2) = '84' THEN '0' || substring(clean_phone from 3)
                ELSE clean_phone
            END
            FROM (
                SELECT id, regexp_replace(phone, '[[:space:].()-]', '', 'g') AS clean_phone
                FROM users WHERE phone IS NOT NULL
            ) AS normalized
            WHERE users.id = normalized.id;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM users
                    WHERE phone IS NOT NULL AND is_deleted = false
                    GROUP BY phone HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Resolve duplicate non-deleted phone numbers before applying AddAuthenLogin.';
                END IF;
            END $$;
            """);

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                replaced_by_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "FK_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "users_phone_active_key",
            table: "users",
            column: "phone",
            unique: true,
            filter: "phone IS NOT NULL AND is_deleted = false");

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_token_hash",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_user_id",
            table: "refresh_tokens",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "refresh_tokens");
        migrationBuilder.DropIndex(name: "users_phone_active_key", table: "users");
    }
}
