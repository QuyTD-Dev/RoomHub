using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.RoomPosts;

namespace Application.Interfaces.Services
{
    public interface IFavoriteRoomService
    {

        Task<bool> ToggleFavoriteAsync(string userId, int roomId);

        Task<IEnumerable<int>> GetUserFavoriteRoomIdsAsync(string userId);

        Task<IEnumerable<RoomListViewModel>> GetFavoriteRoomsForUserAsync(string userId);
    }
}