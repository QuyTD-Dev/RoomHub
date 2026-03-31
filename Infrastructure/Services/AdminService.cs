using Application.DTOs.Admin;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Enums;

namespace Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repository;
        private const int PageSize = 10;

        public AdminService(IAdminRepository repository)
        {
            _repository = repository;
        }

        // ========== DASHBOARD ==========

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var totalRooms = await _repository.CountRoomsAsync();
            var activeContracts = await _repository.CountActiveContractsAsync();
            var totalRevenue = await _repository.GetTotalRevenueAsync();
            var openTickets = await _repository.CountOpenTicketsAsync();
            var totalUsers = await _repository.CountUsersAsync();
            var totalBuildings = await _repository.CountBuildingsAsync();
            var occupancyRate = await _repository.GetOccupancyRateAsync();
            var revenueData = await _repository.GetRevenueLast30DaysAsync();
            var topRooms = await _repository.GetTopRoomsAsync(5);
            var recentUsers = await _repository.GetRecentUsersAsync(5);

            return new DashboardViewModel
            {
                TotalRooms = totalRooms,
                ActiveContracts = activeContracts,
                TotalRevenue = totalRevenue,
                OpenTickets = openTickets,
                TotalUsers = totalUsers,
                TotalBuildings = totalBuildings,
                OccupancyRate = occupancyRate,
                RevenueLast30Days = revenueData.Select(r => new RevenueDataPoint
                {
                    Date = r.Date.ToString("dd/MM"),
                    Amount = r.Amount
                }).ToList(),
                TopViewedRooms = topRooms.Select(r => new TopRoomViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Address = r.Floor?.Building?.Address ?? "N/A",
                    BasePrice = r.BasePrice,
                    Status = r.Status.ToString(),
                    ReviewCount = r.Reviews?.Count ?? 0
                }).ToList(),
                RecentUsers = recentUsers.Select(r => new RecentUserViewModel
                {
                    Id = r.User.Id,
                    FullName = r.User.FullName,
                    Email = r.User.Email,
                    Role = r.Role,
                    CreatedAt = r.User.CreatedAt
                }).ToList()
            };
        }

        // ========== USERS ==========

        public async Task<UserListViewModel> GetUsersAsync(string? role, string? search, int page)
        {
            var (users, totalCount) = await _repository.GetUsersAsync(role, search, page, PageSize);

            return new UserListViewModel
            {
                Users = users.Select(u => new UserItemViewModel
                {
                    Id = u.User.Id,
                    FullName = u.User.FullName,
                    Email = u.User.Email,
                    PhoneNumber = u.User.PhoneNumber,
                    AvatarUrl = u.User.AvatarUrl,
                    Role = u.Role,
                    IsVerified = u.User.IsVerified,
                    IsBanned = u.User.IsBanned,
                    IsDeleted = u.User.IsDeleted,
                    CreatedAt = u.User.CreatedAt
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize),
                TotalCount = totalCount,
                CurrentRole = role,
                CurrentSearch = search
            };
        }

        public async Task<bool> ToggleVerificationAsync(string userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsVerified = !user.IsVerified;
            user.VerificationDate = user.IsVerified ? DateTime.UtcNow : null;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> ToggleBanAsync(string userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsBanned = !user.IsBanned;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateUserAsync(user);
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(string userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateUserAsync(user);
            return true;
        }

        // ========== BUILDINGS ==========

        public async Task<BuildingListViewModel> GetBuildingsAsync(string? search, int page)
        {
            var (buildings, totalCount) = await _repository.GetBuildingsAsync(search, page, PageSize);

            return new BuildingListViewModel
            {
                Buildings = buildings.Select(b => new BuildingItemViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    City = b.City,
                    District = b.District,
                    OwnerName = b.Owner?.FullName ?? "N/A",
                    FloorCount = b.Floors?.Count ?? 0,
                    RoomCount = b.Floors?.Sum(f => f.Rooms?.Count(r => !r.IsDeleted) ?? 0) ?? 0,
                    CreatedAt = b.CreatedAt,
                    IsDeleted = b.IsDeleted
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize),
                TotalCount = totalCount,
                CurrentSearch = search
            };
        }

        // ========== ROOMS ==========

        public async Task<RoomListAdminViewModel> GetRoomsAsync(RoomStatus? status, string? search, int page)
        {
            var (rooms, totalCount) = await _repository.GetAllRoomsAsync(status, search, page, PageSize);

            return new RoomListAdminViewModel
            {
                Rooms = rooms.Select(r => new RoomItemAdminViewModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    RoomNumber = r.RoomNumber,
                    BuildingName = r.Floor?.Building?.Name ?? "N/A",
                    FloorNumber = r.Floor?.FloorNumber ?? 0,
                    BasePrice = r.BasePrice,
                    SurfaceArea = r.SurfaceArea,
                    Status = r.Status,
                    LandlordName = r.Landlord?.FullName ?? "N/A",
                    CreatedAt = r.CreatedAt,
                    IsDeleted = r.IsDeleted
                }).ToList(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize),
                TotalCount = totalCount,
                CurrentStatus = status,
                CurrentSearch = search
            };
        }

        public async Task<bool> ApproveRoomAsync(int roomId)
        {
            var room = await _repository.GetRoomByIdAsync(roomId);
            if (room == null) return false;

            room.Status = RoomStatus.Available;
            room.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateRoomAsync(room);
            return true;
        }

        public async Task<bool> SoftDeleteRoomAsync(int roomId)
        {
            var room = await _repository.GetRoomByIdAsync(roomId);
            if (room == null) return false;

            room.IsDeleted = true;
            room.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateRoomAsync(room);
            return true;
        }
    }
}
