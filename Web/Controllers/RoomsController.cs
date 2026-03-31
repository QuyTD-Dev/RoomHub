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
        public IActionResult Index()
        {
            return RedirectToAction("Index", "RoomPosts");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            return RedirectToAction("Details", "RoomPosts", new { id });
        }
    }
}
