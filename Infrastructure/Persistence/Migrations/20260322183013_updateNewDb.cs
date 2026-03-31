using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateNewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Buildings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "test-user-id-123");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "Address", "AvatarUrl", "ConcurrencyStamp", "CreatedAt", "DateOfBirth", "Email", "EmailConfirmed", "FullName", "Gender", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "ReviewBlockedUntil", "RoleSpecificData", "SecurityStamp", "UpdatedAt", "UserName", "VerificationDate" },
                values: new object[] { "test-user-id-123", null, null, "66ccc56a-a6cd-4d97-b63a-bb06c2e1766f", new DateTime(2026, 3, 22, 15, 22, 47, 25, DateTimeKind.Utc).AddTicks(6133), null, "owner@roomhub.com", true, "Chủ Trọ Test", null, null, "OWNER@ROOMHUB.COM", "OWNER_TEST", "AQAAAAEAACcQAAAAE...", "0123456789", null, null, "7ef8127a-6823-4140-a08c-6cdfc805b80a", null, "owner_test", null });

            migrationBuilder.InsertData(
                table: "Buildings",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "District", "ElectricityPrice", "GarbagePrice", "InternetPrice", "Latitude", "Longitude", "Name", "OwnerId", "Province", "UpdatedAt", "Ward", "WaterPrice" },
                values: new object[] { 1, "123 Đường Test", "Hồ Chí Minh", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quận 1", 0m, 0m, 0m, null, null, "Chung cư mini RoomHub", "test-user-id-123", null, null, "Phường Bến Nghé", 0m });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "BasePrice", "CreatedAt", "Description", "ElectricityPrice", "FloorId", "GarbagePrice", "InternetPrice", "IsFurnished", "IsPublished", "LandlordId", "MaxCapacity", "RoomNumber", "RoomType", "Status", "SurfaceArea", "Title", "UpdatedAt", "WaterPrice" },
                values: new object[] { 1, 3500000m, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng mới xây, dọn vào ở ngay.", null, 1, null, null, true, false, "test-user-id-123", 2, "101", "Other", "Available", 25.5m, "Phòng trọ cao cấp cửa sổ thoáng mát", null, null });
        }
    }
}
