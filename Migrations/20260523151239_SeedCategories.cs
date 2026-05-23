using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DeploymentAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Core academic research documents, programming resources, and computer hardware systems.", "Computer Science & Engineering" },
                    { 2, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Coding puzzle guides, tree traversals, search/sort complexities, and graph concepts.", "Data Structures & Algorithms" },
                    { 3, new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), "CPU scheduling notes, disk optimization techniques, page allocations, and process forks.", "Operating Systems" },
                    { 4, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Relational database structures, SQL query optimization guides, transaction isolation properties.", "Database Management System (DBMS)" },
                    { 5, new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Set theories, logical calculations, probability graphs, and discrete mathematics guides.", "Discrete Mathematics" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Role" },
                values: new object[] { new DateTime(2026, 5, 23, 15, 12, 39, 451, DateTimeKind.Utc).AddTicks(8779), "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Role" },
                values: new object[] { new DateTime(2026, 5, 20, 10, 41, 54, 66, DateTimeKind.Utc).AddTicks(9011), "User" });
        }
    }
}
