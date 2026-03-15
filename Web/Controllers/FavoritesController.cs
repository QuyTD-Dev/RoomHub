using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Tenant")] // Chỉ Tenant mới được dùng tính năng này
    public class FavoritesController : Controller
    {
        private readonly IFavoriteRoomService _favoriteRoomService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(IFavoriteRoomService favoriteRoomService, UserManager<ApplicationUser> userManager)
        {
            _favoriteRoomService = favoriteRoomService;
            _userManager = userManager;
        }

        // GET: /Favorites/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var favorites = await _favoriteRoomService.GetFavoriteRoomsForUserAsync(userId);

            // Truyền danh sách phòng đã yêu thích ra View
            return View(favorites);
        }

        // POST: /Favorites/ToggleFavorite
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật chống CSRF
        public async Task<IActionResult> ToggleFavorite([FromBody] int roomId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu bài đăng." });
            }

            var isFavorited = await _favoriteRoomService.ToggleFavoriteAsync(userId, roomId);
            return Json(new { success = true, isFavorited });
        }
    }
}