using Microsoft.EntityFrameworkCore.Migrations;
using Mooncake.EcommercePlatform.Domain.Enums;

#nullable disable

namespace Mooncake.EcommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<UserRole>(
                name: "requested_role",
                table: "users",
                type: "user_role",
                nullable: false,
                defaultValue: UserRole.Customer);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requested_role",
                table: "users");
        }
    }
}
