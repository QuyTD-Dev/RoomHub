using Application.DTOs.RoomPosts;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "PropertyOwner")]
    public class RoomPostsController : Controller
    {
        private readonly IRoomPostService _roomPostService;
        private readonly IReviewService _reviewService;

        public RoomPostsController(IRoomPostService roomPostService, IReviewService reviewService)
        {
            _roomPostService = roomPostService;
            _reviewService = reviewService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
            //return "test-user-id-123";
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? q, string? province, Domain.Enums.RoomType? roomType, decimal? minPrice, decimal? maxPrice, string? district, string? sortBy, int page = 1)
        {
            int pageSize = 12;
            var paginatedRooms = await _roomPostService.GetAllRoomsAsync(q, province, roomType, minPrice, maxPrice, district, sortBy, page, pageSize);

            ViewBag.isSearching = !string.IsNullOrWhiteSpace(q) || !string.IsNullOrWhiteSpace(province) || roomType.HasValue || minPrice.HasValue || maxPrice.HasValue || !string.IsNullOrWhiteSpace(district);
            ViewBag.searchQuery = q;
            ViewBag.province = province;
            ViewBag.roomType = roomType;
            ViewBag.minPrice = minPrice;
            ViewBag.maxPrice = maxPrice;
            ViewBag.district = district;
            ViewBag.sortBy = sortBy;
            ViewBag.CurrentPage = paginatedRooms.PageIndex;
            ViewBag.TotalPages = paginatedRooms.TotalPages;

            return View(paginatedRooms);
        }

        public async Task<IActionResult> MyPosts()
        {
            var userId = GetUserId();
            var rooms = await _roomPostService.GetMyRoomsAsync(userId);
            return View(rooms);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var viewModel = await _roomPostService.GetRoomDetailsAsync(id);
                var reviews = await _reviewService.GetRootReviewsByRoomAsync(id);
                viewModel.Reviews = reviews.ToList();
                return View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // 1. HÀM GET: Hiển thị Form Đăng tin (Chỉ lấy phòng chưa đăng)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchSuggestions(string q, string? province)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(new List<object>());

            var suggestions = await _roomPostService.GetSuggestionsAsync(q.Trim(), province, 6);
            return Json(suggestions);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? roomId = null)
        {
            var userId = GetUserId();
            // Gọi service để lấy ra danh sách các phòng "Trống & Chưa xuất bản"
            var viewModel = await _roomPostService.GetCreateViewModelAsync(userId);
            
            if (roomId.HasValue)
            {
                viewModel.SelectedRoomId = roomId.Value;
            }

            // Trả ViewModel mới này ra View
            return View(viewModel);
        }

        // 2. HÀM POST: Nhận dữ liệu và Bật công tắc đăng tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoomViewModel model)
        {
            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                // Nếu lỗi, phải nạp lại danh sách phòng vào Dropdown để UI không bị sập
                var viewModel = await _roomPostService.GetCreateViewModelAsync(userId);
                model.AvailableRooms = viewModel.AvailableRooms;
                return View(model);
            }

            try
            {
                // Gọi hàm xuất bản thay vì tạo mới
                await _roomPostService.PublishRoomAsync(model, userId);
                TempData["Success"] = "Đăng tin thành công lên Trang chủ!";
                return RedirectToAction(nameof(MyPosts));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi đăng tin: " + ex.Message;
                var viewModel = await _roomPostService.GetCreateViewModelAsync(userId);
                model.AvailableRooms = viewModel.AvailableRooms;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();
            try
            {
                var viewModel = await _roomPostService.GetEditViewModelAsync(id, userId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditRoomViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var userId = GetUserId();

            if (!ModelState.IsValid)
            {
                var viewModel = await _roomPostService.GetEditViewModelAsync(id, userId);
                model.AvailableFloors = viewModel.AvailableFloors;
                model.AvailableAmenities = viewModel.AvailableAmenities;
                return View(model);
            }

            try
            {
                await _roomPostService.UpdateRoomAsync(model, userId);
                TempData["Success"] = "Room post updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating room post: " + ex.Message;
                var viewModel = await _roomPostService.GetEditViewModelAsync(id, userId);
                model.AvailableFloors = viewModel.AvailableFloors;
                model.AvailableAmenities = viewModel.AvailableAmenities;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            try
            {
                await _roomPostService.DeleteRoomAsync(id, userId);
                TempData["Success"] = "Room post deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting room post: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
