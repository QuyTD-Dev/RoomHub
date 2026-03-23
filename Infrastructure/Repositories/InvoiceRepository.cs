using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Room>> GetRoomsForUtilityInputAsync(int buildingId, string landlordId)
        {
            return await _context.Rooms
                .Include(r => r.Floor)
                    .ThenInclude(f => f.Building)
                .Include(r => r.Contracts.Where(c => c.Status == ContractStatus.Active))
                .Where(r => r.Floor.BuildingId == buildingId
                         && r.LandlordId == landlordId
                         && r.Status == RoomStatus.Occupied
                         && !r.IsDeleted)
                .ToListAsync();
        }

        // [ĐÃ SỬA]: Lấy 2 dòng riêng biệt cho Điện và Nước của tháng trước
        public async Task<List<UtilityReading>> GetLastUtilityReadingsAsync(int contractId)
        {
            var elec = await _context.UtilityReadings
                .Where(u => u.ContractId == contractId && u.UtilityType == UtilityType.Electricity)
                // [ĐÃ SỬA]: Dùng Id để luôn lấy ra chính xác bản ghi chốt sổ cuối cùng
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            var water = await _context.UtilityReadings
                .Where(u => u.ContractId == contractId && u.UtilityType == UtilityType.Water)
                // [ĐÃ SỬA]
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            var list = new List<UtilityReading>();
            if (elec != null) list.Add(elec);
            if (water != null) list.Add(water);
            return list;
        }

        public async Task<bool> SaveInvoicesAndReadingsAsync(List<Invoice> invoices, List<UtilityReading> readings)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (readings.Any()) await _context.UtilityReadings.AddRangeAsync(readings);
                if (invoices.Any()) await _context.Invoices.AddRangeAsync(invoices);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<Invoice>> GetInvoicesByLandlordAsync(string landlordId, int? buildingId, int? month, int? year, InvoiceStatus? status = null)
        {
            var query = _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                        .ThenInclude(r => r.Floor)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Tenant)
                .Where(i => i.Contract.OwnerId == landlordId);

            if (buildingId.HasValue) query = query.Where(i => i.Contract.Room.Floor.BuildingId == buildingId.Value);
            if (month.HasValue) query = query.Where(i => i.InvoiceDate.Month == month.Value);
            if (year.HasValue) query = query.Where(i => i.InvoiceDate.Year == year.Value);

            // [BỔ SUNG]: Lọc theo Trạng thái (Status)
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);

            return await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Owner) // Lấy info chủ nhà để hiện tên lên QR
                .Include(i => i.InvoiceItems) // Lấy bảng kê chi tiết
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Invoice>> GetInvoicesByTenantAsync(string tenantId)
        {
            return await _context.Invoices
                .Include(i => i.Contract)
                    .ThenInclude(c => c.Room)
                .Where(i => i.Contract.TenantId == tenantId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }
    }
}