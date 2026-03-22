using Application.DTOs.Admin;
using Domain.Enums;

namespace Application.Interfaces.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<DashboardViewModel> GetDashboardAsync();

        // Users
        Task<UserListViewModel> GetUsersAsync(string? role, string? search, int page);
        Task<bool> ToggleVerificationAsync(string userId);
        Task<bool> ToggleBanAsync(string userId);
        Task<bool> SoftDeleteUserAsync(string userId);

        // Buildings
        Task<BuildingListViewModel> GetBuildingsAsync(string? search, int page);

        // Rooms
        Task<RoomListAdminViewModel> GetRoomsAsync(RoomStatus? status, string? search, int page);
        Task<bool> ApproveRoomAsync(int roomId);
        Task<bool> SoftDeleteRoomAsync(int roomId);
    }
}
