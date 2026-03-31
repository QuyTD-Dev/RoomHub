using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Administrator")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IAIService _aiService;

        public AdminController(IAdminService adminService, IAIService aiService)
        {
            _adminService = adminService;
            _aiService = aiService;
        }

        // GET /Admin — Dashboard
        public async Task<IActionResult> Index()
        {
            var vm = await _adminService.GetDashboardAsync();

            // AI: Dashboard Insights + Sentiment
            var insightsTask = _aiService.GetDashboardInsightsAsync(vm);
            var sentimentTask = _aiService.GetReviewSentimentSummaryAsync();

            await Task.WhenAll(insightsTask, sentimentTask);

            vm.AiInsights = insightsTask.Result;

            var sentiment = sentimentTask.Result;
            vm.SentimentSummary = sentiment.Summary;
            vm.SentimentPositive = sentiment.Positive;
            vm.SentimentNeutral = sentiment.Neutral;
            vm.SentimentNegative = sentiment.Negative;

            return View(vm);
        }

        // GET /Admin/Users
        public async Task<IActionResult> Users(string? role, string? search, int page = 1)
        {
            var vm = await _adminService.GetUsersAsync(role, search, page);

            // AI: User Risk Analysis
            vm.UserRisks = await _aiService.GetUserRiskAnalysisAsync(vm.Users.ToList());

            return View(vm);
        }

        // POST /Admin/ToggleVerify/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVerify(string id)
        {
            await _adminService.ToggleVerificationAsync(id);
            TempData["Success"] = "Cập nhật trạng thái xác thực thành công.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/ToggleBan/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(string id)
        {
            await _adminService.ToggleBanAsync(id);
            TempData["Success"] = "Cập nhật trạng thái ban thành công.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _adminService.SoftDeleteUserAsync(id);
            TempData["Success"] = "Đã xóa người dùng.";
            return RedirectToAction(nameof(Users));
        }

        // GET /Admin/Buildings
        public async Task<IActionResult> Buildings(string? search, int page = 1)
        {
            var vm = await _adminService.GetBuildingsAsync(search, page);
            return View(vm);
        }

        // GET /Admin/Rooms
        public async Task<IActionResult> Rooms(RoomStatus? status, string? search, int page = 1)
        {
            var vm = await _adminService.GetRoomsAsync(status, search, page);

            // AI: Price Suggestions
            vm.PriceSuggestions = await _aiService.GetPriceSuggestionsAsync(vm.Rooms.ToList());

            return View(vm);
        }

        // POST /Admin/ApproveRoom/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRoom(int id)
        {
            await _adminService.ApproveRoomAsync(id);
            TempData["Success"] = "Phòng đã được duyệt.";
            return RedirectToAction(nameof(Rooms));
        }

        // POST /Admin/DeleteRoom/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            await _adminService.SoftDeleteRoomAsync(id);
            TempData["Success"] = "Phòng đã được xóa.";
            return RedirectToAction(nameof(Rooms));
        }
    }
}
