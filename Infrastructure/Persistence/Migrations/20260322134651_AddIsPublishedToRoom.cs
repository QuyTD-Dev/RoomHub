using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublishedToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "fdfc974a-1119-4bc4-bc41-3a391396cae6", new DateTime(2026, 3, 22, 13, 46, 50, 860, DateTimeKind.Utc).AddTicks(2107), "6638fb09-2d12-441e-bb12-cec0cffad8c1" });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsPublished",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Rooms");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "9ff9f66d-7181-4f7a-9708-1bad74a35a05", new DateTime(2026, 3, 22, 9, 41, 10, 383, DateTimeKind.Utc).AddTicks(6766), "48a25f99-1508-4882-ab05-1ff807c3de55" });
        }
    }
}
