using Application.DTOs.Buildings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class BuildingService : IBuildingService
    {
        private readonly IBuildingRepository _buildingRepository;

        public BuildingService(IBuildingRepository buildingRepository)
        {
            _buildingRepository = buildingRepository;
        }

        public async Task<List<BuildingListDto>> GetMyBuildingsAsync(string ownerId)
        {
            // 1. Lấy dữ liệu thô từ Database
            var buildings = await _buildingRepository.GetBuildingsByOwnerAsync(ownerId);

            var result = new List<BuildingListDto>();

            // 2. Chế biến dữ liệu để đẩy ra màn hình (Đếm số phòng trống/đang thuê)
            foreach (var b in buildings)
            {
                var allRooms = b.Floors.SelectMany(f => f.Rooms).ToList();
                int total = allRooms.Count;
                int available = allRooms.Count(r => r.Status == RoomStatus.Available);
                int occupied = allRooms.Count(r => r.Status == RoomStatus.Occupied);

                result.Add(new BuildingListDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    // Format lại địa chỉ cho đẹp
                    Address = $"{b.Address}, {b.Ward}, {b.District}, {b.City}",
                    TotalRooms = total,
                    AvailableRooms = available,
                    OccupiedRooms = occupied,
                    ThumbnailUrl = null
                });
            }
            return result;
        }

        public async Task<int> CreateBuildingAsync(string ownerId, CreateBuildingDto config, FloorSetupDto structure)
        {
            // 1. Khởi tạo Tòa nhà (Ánh xạ những field có sẵn trong Building.cs của bạn)
            var building = new Building
            {
                OwnerId = ownerId,
                Name = config.Name,
                Province = config.Province,
                City = config.Province,
                District = config.District,
                Ward = config.Ward,
                Address = config.StreetAddress,

                // [ĐÃ SỬA] - BỔ SUNG 4 DÒNG NÀY ĐỂ KHÔNG BỊ MẤT GIÁ TIỀN:
                ElectricityPrice = config.ElectricityPrice,
                WaterPrice = config.WaterPrice,
                InternetPrice = config.InternetPrice,
                GarbagePrice = config.GarbagePrice,

                CreatedAt = DateTime.UtcNow
            };

            var floors = new List<Floor>();
            var rooms = new List<Room>();

            // 2. THUẬT TOÁN SINH TẦNG VÀ PHÒNG TỰ ĐỘNG
            for (int i = 1; i <= structure.NumberOfFloors; i++)
            {
                // Tạo đối tượng Tầng
                var floor = new Floor
                {
                    FloorNumber = i,
                    CreatedAt = DateTime.UtcNow
                };
                floors.Add(floor);

                // Kiểm tra xem tầng này có bị "chủ nhà tùy chỉnh" số lượng phòng hay không?
                int roomCount = structure.RoomsPerFloor;
                var customFloor = structure.CustomFloors?.FirstOrDefault(c => c.FloorNumber == i);
                if (customFloor != null)
                {
                    roomCount = customFloor.NumberOfRooms;
                }

                // Chạy vòng lặp tạo Phòng cho Tầng hiện tại
                for (int j = 1; j <= roomCount; j++)
                {
                    // Công thức tạo tên phòng: Tầng * 100 + số thứ tự phòng (Ví dụ: 2 * 100 + 3 = 203)
                    string roomName = (i * 100 + j).ToString();

                    var room = new Room
                    {
                        Floor = floor,
                        LandlordId = ownerId,
                        RoomNumber = roomName,
                        Title = $"Phòng {roomName}",
                        Status = RoomStatus.Available,
                        IsPublished = false, // Luôn mặc định là false khi vừa sinh ra
                        BasePrice = 0,
                        MaxCapacity = 2,
                        IsFurnished = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    rooms.Add(room);
                }
            }

            // 3. Giao việc cho Repository lưu toàn bộ đống này xuống Database bằng Transaction
            var savedBuilding = await _buildingRepository.CreateBuildingWithStructureAsync(building, floors, rooms);

            return savedBuilding.Id;
        }
    }
}