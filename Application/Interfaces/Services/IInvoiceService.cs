using Application.DTOs.Billing;
using static Application.DTOs.Billing.InvoiceListViewModel;

namespace Application.Interfaces.Services
{
    public interface IInvoiceService
    {
        // Lấy danh sách các phòng cần chốt số điện nước trong 1 tòa nhà vào 1 tháng cụ thể
        Task<List<RoomUtilityInputViewModel>> GetRoomsForUtilityInputAsync(int buildingId, int month, int year, string landlordId);

        // Thuật toán: Nhận chỉ số mới -> Tính toán -> Sinh Hóa đơn (Invoice)
        Task<bool> GenerateMonthlyInvoicesAsync(int buildingId, List<RecordUtilityDto> readings, string landlordId);
        Task<List<InvoiceListViewModel>> GetInvoicesAsync(string landlordId, int? buildingId, int? month, int? year, Domain.Enums.InvoiceStatus? status = null);
        Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, string landlordId);
        Task<List<InvoiceListViewModel>> GetTenantInvoicesAsync(string tenantId);
        Task<InvoiceDetailViewModel> GetInvoiceDetailAsync(int invoiceId, string tenantId);
        Task<bool> SubmitPaymentProofAsync(int invoiceId, string tenantId, string proofUrl);
    }
}