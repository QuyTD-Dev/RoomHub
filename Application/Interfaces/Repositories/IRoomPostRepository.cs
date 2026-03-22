using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IRoomPostRepository
    {
        Task<IEnumerable<Room>> GetAllActiveAsync();
        Task<IEnumerable<Room>> GetByLandlordIdAsync(string landlordId);
        Task<Room?> GetByIdAsync(int id);
        Task AddAsync(Room room);
        Task UpdateAsync(Room room);
        Task DeleteAsync(Room room);

        // Lookup data helpers
        Task<IEnumerable<Floor>> GetFloorsByLandlordIdAsync(string landlordId);
        Task<IEnumerable<Amenity>> GetAllAmenitiesAsync();

        Task<IEnumerable<Room>> GetUnpublishedRoomsByLandlordIdAsync(string landlordId);
        // Public browsing
        Task<IEnumerable<Room>> GetAvailableRoomsAsync();
        Task<Room?> GetRoomDetailsByIdAsync(int id);

        // Search & Filter
        Task<IEnumerable<Room>> SearchAsync(string? keyword, string? province, Domain.Enums.RoomType? roomType = null);
        Task<(IEnumerable<Room> Items, int TotalCount)> PaginatedSearchAsync(string? keyword, string? province, Domain.Enums.RoomType? roomType, int pageIndex, int pageSize);
    }
}
