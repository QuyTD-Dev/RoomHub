using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewBlockedUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "b1ffdf74-2e44-4b12-9f00-8298148408c3", new DateTime(2026, 3, 22, 11, 1, 14, 257, DateTimeKind.Utc).AddTicks(7186), "d5101c7d-cd75-41dd-8db7-7b7d27101b1c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "62819383-1c7e-4dc2-bb21-67e84474a43c", new DateTime(2026, 3, 22, 10, 37, 33, 978, DateTimeKind.Utc).AddTicks(7251), "233e10fa-46a8-4946-8605-3e945268f19c" });
        }
    }
}
