using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.RoomPosts;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

namespace Application.Services
{
    public class FavoriteRoomService : IFavoriteRoomService
    {
        private readonly IFavoriteRoomRepository _favoriteRoomRepo;

        public FavoriteRoomService(IFavoriteRoomRepository favoriteRoomRepo)
        {
            _favoriteRoomRepo = favoriteRoomRepo;
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int roomId)
        {
            return await _favoriteRoomRepo.ToggleFavoriteAsync(userId, roomId);
        }

        public async Task<IEnumerable<int>> GetUserFavoriteRoomIdsAsync(string userId)
        {
            return await _favoriteRoomRepo.GetUserFavoriteRoomIdsAsync(userId);
        }

        public async Task<IEnumerable<RoomListViewModel>> GetFavoriteRoomsForUserAsync(string userId)
        {
            var rooms = await _favoriteRoomRepo.GetUserFavoritesAsync(userId);

            return rooms.Select(r => new RoomListViewModel
            {
                Id = r.Id,
                Title = r.Title,
                BasePrice = r.BasePrice,
                SurfaceArea = r.SurfaceArea,
                Address = r.Floor?.Building?.Address ?? "Chưa cập nhật",
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                RoomNumber = r.RoomNumber,
                RoomType = r.RoomType,
                AmenityCount = r.RoomAmenities?.Count ?? 0,
                IsFavorited = true // Chắc chắn là true vì đang lấy trong danh sách yêu thích
            });
        }
    }
}