using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class FavoriteRoomRepository : IFavoriteRoomRepository
    {
        private readonly ApplicationDbContext _context;

        public FavoriteRoomRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ToggleFavoriteAsync(string userId, int roomId)
        {
            var existingFavorite = await _context.FavoriteRooms
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RoomId == roomId);

            if (existingFavorite != null)
            {
                _context.FavoriteRooms.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return false;
            }

            var newFavorite = new FavoriteRoom
            {
                UserId = userId,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.FavoriteRooms.AddAsync(newFavorite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<int>> GetUserFavoriteRoomIdsAsync(string userId)
        {
            return await _context.FavoriteRooms
                .Where(f => f.UserId == userId)
                .Select(f => f.RoomId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetUserFavoritesAsync(string userId)
        {
            return await _context.FavoriteRooms
                .Where(f => f.UserId == userId)
                .Include(f => f.Room) 
                .Select(f => f.Room)
                .ToListAsync();
        }
    }
}