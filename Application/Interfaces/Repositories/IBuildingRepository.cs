using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IBuildingRepository
    {
        // Lấy danh sách các tòa nhà của một chủ nhà
        Task<List<Building>> GetBuildingsByOwnerAsync(string ownerId);

        // Lấy chi tiết 1 tòa nhà (bao gồm cả Tầng và Phòng bên trong)
        Task<Building?> GetBuildingDetailsAsync(int buildingId, string ownerId);

        // Lưu Tòa nhà, Tầng, Phòng vào Database cùng lúc (Sử dụng Transaction)
        Task<Building> CreateBuildingWithStructureAsync(Building building, List<Floor> floors, List<Room> rooms);
    }
}