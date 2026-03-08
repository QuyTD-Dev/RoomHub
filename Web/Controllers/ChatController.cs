using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class ChatController : Controller
    {
        // Giao diện Chat dành cho Người thuê nhà
        public IActionResult Tenant()
        {
            return View();
        }

        // Giao diện Chat dành cho Chủ nhà
        public IActionResult Owner()
        {
            return View();
        }
    }
}