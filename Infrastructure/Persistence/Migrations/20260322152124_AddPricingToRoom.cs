using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "17023071-5250-499a-bae3-39f58b910747", new DateTime(2026, 3, 22, 15, 21, 23, 827, DateTimeKind.Utc).AddTicks(9492), "3a0c35c7-7376-48e2-b65b-00873d0e3f92" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "c7a046d8-39cc-42b8-bed4-08a38a63cc7c", new DateTime(2026, 3, 22, 15, 6, 45, 355, DateTimeKind.Utc).AddTicks(4519), "cdb05976-2760-4729-bb73-9dfe0ad3a646" });
        }
    }
}
