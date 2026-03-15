using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IFavoriteRoomRepository
    {

        Task<bool> ToggleFavoriteAsync(string userId, int roomId);
        Task<IEnumerable<int>> GetUserFavoriteRoomIdsAsync(string userId);
        Task<IEnumerable<Room>> GetUserFavoritesAsync(string userId);
    }
}