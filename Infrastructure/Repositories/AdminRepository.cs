using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ========== DASHBOARD ==========

        public async Task<int> CountRoomsAsync()
            => await _context.Rooms.CountAsync(r => !r.IsDeleted);

        public async Task<int> CountActiveContractsAsync()
            => await _context.Contracts.CountAsync(c => c.Status == ContractStatus.Active && !c.IsDeleted);

        public async Task<decimal> GetTotalRevenueAsync()
            => await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);

        public async Task<int> CountOpenTicketsAsync()
            => await _context.MaintenanceTickets.CountAsync(t => t.Status == TicketStatus.Open);

        public async Task<int> CountUsersAsync()
            => await _context.Users.CountAsync(u => !u.IsDeleted);

        public async Task<int> CountBuildingsAsync()
            => await _context.Buildings.CountAsync(b => !b.IsDeleted);

        public async Task<decimal> GetOccupancyRateAsync()
        {
            var total = await _context.Rooms.CountAsync(r => !r.IsDeleted);
            if (total == 0) return 0;
            var occupied = await _context.Rooms.CountAsync(r => !r.IsDeleted && r.Status == RoomStatus.Occupied);
            return Math.Round((decimal)occupied / total * 100, 1);
        }

        public async Task<IEnumerable<(DateTime Date, decimal Amount)>> GetRevenueLast30DaysAsync()
        {
            var since = DateTime.UtcNow.AddDays(-30).Date;
            var payments = await _context.Payments
                .Where(p => p.PaidAt >= since && p.Status == "Completed")
                .GroupBy(p => p.PaidAt!.Value.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return payments.Select(p => (p.Date, p.Amount));
        }

        public async Task<IEnumerable<Room>> GetTopRoomsAsync(int count)
        {
            return await _context.Rooms
                .Include(r => r.Floor).ThenInclude(f => f.Building)
                .Include(r => r.Reviews)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.Reviews.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<(ApplicationUser User, string Role)>> GetRecentUsersAsync(int count)
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<(ApplicationUser, string)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add((user, roles.FirstOrDefault() ?? "N/A"));
            }
            return result;
        }

        // ========== USERS ==========

        public async Task<(IEnumerable<(ApplicationUser User, string Role)> Users, int TotalCount)> GetUsersAsync(
            string? role, string? search, int page, int pageSize)
        {
            IQueryable<ApplicationUser> query = _context.Users;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            // If role filter, get user ids in that role first
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => r.Name == role);
                if (roleEntity != null)
                {
                    var userIdsInRole = _context.UserRoles
                        .Where(ur => ur.RoleId == roleEntity.Id)
                        .Select(ur => ur.UserId);
                    query = query.Where(u => userIdsInRole.Contains(u.Id));
                }
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<(ApplicationUser, string)>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add((user, roles.FirstOrDefault() ?? "N/A"));
            }

            return (result, totalCount);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
            => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task UpdateUserAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        // ========== BUILDINGS ==========

        public async Task<(IEnumerable<Building> Buildings, int TotalCount)> GetBuildingsAsync(
            string? search, int page, int pageSize)
        {
            IQueryable<Building> query = _context.Buildings
                .Include(b => b.Owner)
                .Include(b => b.Floors)
                    .ThenInclude(f => f.Rooms);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(s) ||
                    b.Address.ToLower().Contains(s) ||
                    b.City.ToLower().Contains(s));
            }

            query = query.Where(b => !b.IsDeleted);

            var totalCount = await query.CountAsync();

            var buildings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (buildings, totalCount);
        }

        // ========== ROOMS ==========

        public async Task<(IEnumerable<Room> Rooms, int TotalCount)> GetAllRoomsAsync(
            RoomStatus? status, string? search, int page, int pageSize)
        {
            IQueryable<Room> query = _context.Rooms
                .Include(r => r.Floor).ThenInclude(f => f.Building)
                .Include(r => r.Landlord);

            query = query.Where(r => !r.IsDeleted);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(s) ||
                    r.RoomNumber.ToLower().Contains(s));
            }

            var totalCount = await query.CountAsync();

            var rooms = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (rooms, totalCount);
        }

        public async Task<Room?> GetRoomByIdAsync(int id)
            => await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

        public async Task UpdateRoomAsync(Room room)
        {
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
        }
    }
}
