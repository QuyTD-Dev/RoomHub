using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Demo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "43f8513b-7c13-4cdf-a549-ad725e8d96ea", new DateTime(2026, 3, 14, 8, 39, 6, 107, DateTimeKind.Utc).AddTicks(6408), "16502d77-d49f-42e1-b005-3cf7badff632" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "6b655961-ff6c-4479-99cd-0cf667fb9a94", new DateTime(2026, 3, 9, 9, 58, 52, 101, DateTimeKind.Utc).AddTicks(9532), "528cbd7e-b24c-42ff-84eb-0ed9d3c34fd8" });
        }
    }
}
