using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeploymentAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswordSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "Role" },
                values: new object[] { new DateTime(2026, 5, 20, 10, 41, 54, 66, DateTimeKind.Utc).AddTicks(9011), "$2b$11$Km2MxlVBhCHANnzP7D8DO.IXD1yr48fGA0gHj7M2U5ZcC7Pi55HfW", "User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash", "Role" },
                values: new object[] { new DateTime(2026, 5, 16, 6, 18, 59, 165, DateTimeKind.Utc).AddTicks(5860), "$2a$11$B8G6YpHPeS8fqF8qF8qF8uL8L8L8L8L8L8L8L8L8L8L8L8L8L8L8L8", "Admin" });
        }
    }
}
