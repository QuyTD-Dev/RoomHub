using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories
{
    public interface IAdminRepository
    {
        // Dashboard
        Task<int> CountRoomsAsync();
        Task<int> CountActiveContractsAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<int> CountOpenTicketsAsync();
        Task<int> CountUsersAsync();
        Task<int> CountBuildingsAsync();
        Task<decimal> GetOccupancyRateAsync();
        Task<IEnumerable<(DateTime Date, decimal Amount)>> GetRevenueLast30DaysAsync();
        Task<IEnumerable<Room>> GetTopRoomsAsync(int count);
        Task<IEnumerable<(ApplicationUser User, string Role)>> GetRecentUsersAsync(int count);

        // Users
        Task<(IEnumerable<(ApplicationUser User, string Role)> Users, int TotalCount)> GetUsersAsync(
            string? role, string? search, int page, int pageSize);
        Task<ApplicationUser?> GetUserByIdAsync(string id);
        Task UpdateUserAsync(ApplicationUser user);

        // Buildings
        Task<(IEnumerable<Building> Buildings, int TotalCount)> GetBuildingsAsync(
            string? search, int page, int pageSize);

        // Rooms
        Task<(IEnumerable<Room> Rooms, int TotalCount)> GetAllRoomsAsync(
            RoomStatus? status, string? search, int page, int pageSize);
        Task<Room?> GetRoomByIdAsync(int id);
        Task UpdateRoomAsync(Room room);
    }
}
