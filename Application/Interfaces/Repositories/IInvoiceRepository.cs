using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IInvoiceRepository
    {
        Task<List<Room>> GetRoomsForUtilityInputAsync(int buildingId, string landlordId);

        // [ĐÃ SỬA]: Lấy danh sách chỉ số cũ theo Hợp đồng (Gồm cả Điện và Nước)
        Task<List<UtilityReading>> GetLastUtilityReadingsAsync(int contractId);

        Task<bool> SaveInvoicesAndReadingsAsync(List<Invoice> invoices, List<UtilityReading> readings);
        Task<List<Invoice>> GetInvoicesByLandlordAsync(string landlordId, int? buildingId, int? month, int? year, Domain.Enums.InvoiceStatus? status = null);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task<List<Invoice>> GetInvoicesByTenantAsync(string tenantId);
        Task SaveNotificationsAsync(List<Notification> notifications);
    }
}