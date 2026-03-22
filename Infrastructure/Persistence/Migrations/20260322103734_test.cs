using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "62819383-1c7e-4dc2-bb21-67e84474a43c", new DateTime(2026, 3, 22, 10, 37, 33, 978, DateTimeKind.Utc).AddTicks(7251), "233e10fa-46a8-4946-8605-3e945268f19c" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "d56f113b-901c-4f6d-bacb-d127619a5034", new DateTime(2026, 3, 22, 10, 33, 48, 751, DateTimeKind.Utc).AddTicks(2493), "91fa3bca-3a3a-40ca-acd4-09331a185add" });
        }
    }
}
