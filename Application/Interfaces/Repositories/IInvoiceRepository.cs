using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IInvoiceRepository
    {
        Task<List<Room>> GetRoomsForUtilityInputAsync(int buildingId, string landlordId);

        // [ĐÃ SỬA]: Lấy danh sách chỉ số cũ theo Hợp đồng (Gồm cả Điện và Nước)
        Task<List<UtilityReading>> GetLastUtilityReadingsAsync(int contractId);

        Task<bool> SaveInvoicesAndReadingsAsync(List<Invoice> invoices, List<UtilityReading> readings);
    }
}