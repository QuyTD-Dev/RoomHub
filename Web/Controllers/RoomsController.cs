using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class RoomsController : Controller
    {
        private readonly IRoomPostService _roomPostService;

        public RoomsController(IRoomPostService roomPostService)
        {
            _roomPostService = roomPostService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rooms = await _roomPostService.GetPublicRoomsAsync();
            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var viewModel = await _roomPostService.GetPublicRoomDetailsAsync(id);
                return View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
