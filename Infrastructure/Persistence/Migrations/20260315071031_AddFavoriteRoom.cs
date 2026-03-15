using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "c9cca490-03c6-4baf-aee3-50408b599b8c", new DateTime(2026, 3, 15, 7, 10, 30, 786, DateTimeKind.Utc).AddTicks(4387), "c3f25748-a359-48ff-a90b-53b4dc7e33b0" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "0d5004b4-ad23-4cf3-b1be-38b55d485a69", new DateTime(2026, 3, 15, 7, 8, 23, 930, DateTimeKind.Utc).AddTicks(8031), "6de0036e-fa9d-4bff-8c12-c085aa4f6bc4" });
        }
    }
}
