using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "3e19051c-e4f1-473c-91be-1e4c9b89f63b", new DateTime(2026, 3, 22, 14, 31, 32, 240, DateTimeKind.Utc).AddTicks(9975), "e22c0f74-bc17-45a5-a5b1-a85c39cf20ee" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "fdfc974a-1119-4bc4-bc41-3a391396cae6", new DateTime(2026, 3, 22, 13, 46, 50, 860, DateTimeKind.Utc).AddTicks(2107), "6638fb09-2d12-441e-bb12-cec0cffad8c1" });
        }
    }
}
