using Application.DTOs.Buildings;

namespace Application.Interfaces.Services
{
    public interface IBuildingService
    {
        // Lấy dữ liệu đã được mông má (DTO) để render ra màn hình danh sách
        Task<List<BuildingListDto>> GetMyBuildingsAsync(string ownerId);

        // Nhận dữ liệu từ 2 Form: Cấu hình chung + Cấu trúc tầng -> Xử lý thuật toán sinh phòng
        Task<int> CreateBuildingAsync(string ownerId, CreateBuildingDto config, FloorSetupDto structure);
    }
}