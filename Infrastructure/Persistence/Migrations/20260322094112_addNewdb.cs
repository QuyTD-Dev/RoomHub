using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addNewdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "9ff9f66d-7181-4f7a-9708-1bad74a35a05", new DateTime(2026, 3, 22, 9, 41, 10, 383, DateTimeKind.Utc).AddTicks(6766), "48a25f99-1508-4882-ab05-1ff807c3de55" });
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
