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
                .OrderByDescending(u => u.ReadingDate)
                .FirstOrDefaultAsync();

            var water = await _context.UtilityReadings
                .Where(u => u.ContractId == contractId && u.UtilityType == UtilityType.Water)
                .OrderByDescending(u => u.ReadingDate)
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
    }
}