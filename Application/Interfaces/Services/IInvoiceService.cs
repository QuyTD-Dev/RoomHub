using Application.DTOs.Billing;

namespace Application.Interfaces.Services
{
    public interface IInvoiceService
    {
        // Lấy danh sách các phòng cần chốt số điện nước trong 1 tòa nhà vào 1 tháng cụ thể
        Task<List<RoomUtilityInputViewModel>> GetRoomsForUtilityInputAsync(int buildingId, int month, int year, string landlordId);

        // Thuật toán: Nhận chỉ số mới -> Tính toán -> Sinh Hóa đơn (Invoice)
        Task<bool> GenerateMonthlyInvoicesAsync(int buildingId, List<RecordUtilityDto> readings, string landlordId);
    }
}