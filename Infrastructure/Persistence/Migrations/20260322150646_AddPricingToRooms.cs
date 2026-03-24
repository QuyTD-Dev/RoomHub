using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingToRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityPrice",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GarbagePrice",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InternetPrice",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterPrice",
                table: "Rooms",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityPrice",
                table: "Buildings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GarbagePrice",
                table: "Buildings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InternetPrice",
                table: "Buildings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterPrice",
                table: "Buildings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "c7a046d8-39cc-42b8-bed4-08a38a63cc7c", new DateTime(2026, 3, 22, 15, 6, 45, 355, DateTimeKind.Utc).AddTicks(4519), "cdb05976-2760-4729-bb73-9dfe0ad3a646" });

            migrationBuilder.UpdateData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ElectricityPrice", "GarbagePrice", "InternetPrice", "WaterPrice" },
                values: new object[] { 0m, 0m, 0m, 0m });

            migrationBuilder.UpdateData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ElectricityPrice", "GarbagePrice", "InternetPrice", "WaterPrice" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityPrice",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GarbagePrice",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "InternetPrice",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "WaterPrice",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ElectricityPrice",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "GarbagePrice",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "InternetPrice",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "WaterPrice",
                table: "Buildings");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "SecurityStamp" },
                values: new object[] { "3e19051c-e4f1-473c-91be-1e4c9b89f63b", new DateTime(2026, 3, 22, 14, 31, 32, 240, DateTimeKind.Utc).AddTicks(9975), "e22c0f74-bc17-45a5-a5b1-a85c39cf20ee" });
        }
    }
}
