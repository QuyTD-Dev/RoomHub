using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BuildingRepository : IBuildingRepository
    {
        private readonly ApplicationDbContext _context;

        public BuildingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Building>> GetBuildingsByOwnerAsync(string ownerId)
        {
            return await _context.Buildings
                .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms)
                // [ĐÃ SỬA]: Dùng OwnerId theo đúng file Building.cs của bạn
                .Where(b => b.OwnerId == ownerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Building?> GetBuildingDetailsAsync(int buildingId, string ownerId)
        {
            return await _context.Buildings
                .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms)
                // [ĐÃ SỬA]: Dùng OwnerId
                .FirstOrDefaultAsync(b => b.Id == buildingId && b.OwnerId == ownerId);
        }

        public async Task<Building> CreateBuildingWithStructureAsync(Building building, List<Floor> floors, List<Room> rooms)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lưu Tòa nhà
                await _context.Buildings.AddAsync(building);
                await _context.SaveChangesAsync(); // Lệnh này giúp sinh ra building.Id

                // 2. Lưu Tầng
                foreach (var floor in floors)
                {
                    floor.BuildingId = building.Id;
                }
                await _context.Floors.AddRangeAsync(floors);
                await _context.SaveChangesAsync(); // Lệnh này giúp sinh ra floor.Id cho từng tầng

                // 3. Lưu Phòng
                foreach (var room in rooms)
                {
                    // [ĐÃ XÓA]: room.BuildingId = building.Id (Vì Room chỉ nối với Floor)

                    // [ĐÃ SỬA]: Gán LandlordId của Room bằng OwnerId của Building
                    room.LandlordId = building.OwnerId;
                }

                if (rooms.Any())
                {
                    await _context.Rooms.AddRangeAsync(rooms);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return building;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteBuildingAsync(int buildingId, string ownerId)
        {
            var building = await _context.Buildings
                .Include(b => b.Floors)
                .ThenInclude(f => f.Rooms)
                .FirstOrDefaultAsync(b => b.Id == buildingId && b.OwnerId == ownerId);

            if (building == null) return false;

            // Kiểm tra xem có phòng nào đang có người ở không
            bool hasOccupiedRooms = building.Floors
                .SelectMany(f => f.Rooms)
                .Any(r => r.Status == Domain.Enums.RoomStatus.Occupied);

            if (hasOccupiedRooms)
            {
                throw new InvalidOperationException("Không thể xóa tòa nhà vì đang có phòng được cho thuê. Vui lòng thanh lý hợp đồng trước.");
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}