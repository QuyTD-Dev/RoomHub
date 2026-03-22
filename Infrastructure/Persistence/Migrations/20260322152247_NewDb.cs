using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "66ccc56a-a6cd-4d97-b63a-bb06c2e1766f", new DateTime(2026, 3, 22, 15, 22, 47, 25, DateTimeKind.Utc).AddTicks(6133), "7ef8127a-6823-4140-a08c-6cdfc805b80a" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "17023071-5250-499a-bae3-39f58b910747", new DateTime(2026, 3, 22, 15, 21, 23, 827, DateTimeKind.Utc).AddTicks(9492), "3a0c35c7-7376-48e2-b65b-00873d0e3f92" });
        }
    }
}
