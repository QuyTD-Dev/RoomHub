using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class demo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "d56f113b-901c-4f6d-bacb-d127619a5034", new DateTime(2026, 3, 22, 10, 33, 48, 751, DateTimeKind.Utc).AddTicks(2493), "91fa3bca-3a3a-40ca-acd4-09331a185add" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "eed97ed5-8f4e-46b0-b75d-b2c275a47e68", new DateTime(2026, 3, 22, 8, 50, 12, 600, DateTimeKind.Utc).AddTicks(5040), "0fdafeca-6e68-4bd2-93fc-6bba907bb1f2" });
        }
    }
}
