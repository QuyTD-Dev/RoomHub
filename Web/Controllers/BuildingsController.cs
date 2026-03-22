using Application.DTOs.Buildings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class BuildingsController : Controller
    {
        private readonly IBuildingService _buildingService;
        private readonly IBuildingRepository _buildingRepository;

        public BuildingsController(IBuildingService buildingService, IBuildingRepository buildingRepository)
        {
            _buildingService = buildingService;
            _buildingRepository = buildingRepository;
        }

        // 1. MÀN HÌNH DANH SÁCH TÒA NHÀ
        public async Task<IActionResult> Index()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var buildings = await _buildingService.GetMyBuildingsAsync(ownerId);
            return View(buildings);
        }

        // 2. MÀN HÌNH TẠO TÒA NHÀ (Hiển thị UI)
        public IActionResult Create()
        {
            return View();
        }

        // 3. API NHẬN DỮ LIỆU TỪ WIZARD BẮN LÊN BẰNG AJAX
        // Đổi [FromBody] thành [FromForm] để nhận File và FormData
        [HttpPost]
        public async Task<IActionResult> CreateData([FromForm] CreateBuildingRequest request)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                // Gọi Service xử lý như bình thường
                // Lưu ý: Lúc này request.Config.Photos đã chứa các file ảnh bạn tải lên.
                // Bạn có thể gọi ICloudinaryService ở đây để lưu ảnh lên Cloud, sau đó gán URL vào DB.

                int buildingId = await _buildingService.CreateBuildingAsync(ownerId, request.Config, request.Structure);
                return Json(new { success = true, buildingId = buildingId, message = "Tạo tòa nhà thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // 4. MÀN HÌNH MA TRẬN PHÒNG (ROOM MATRIX)
        public async Task<IActionResult> Details(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Lấy trực tiếp từ Repo để hiển thị lên View
            var building = await _buildingRepository.GetBuildingDetailsAsync(id, ownerId);

            if (building == null) return NotFound();

            return View(building);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRoomQuickConfig([FromServices] ApplicationDbContext context, [FromBody] UpdateRoomConfigDto request)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tìm phòng và kiểm tra quyền sở hữu
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId && r.LandlordId == ownerId);
            if (room == null) return Json(new { success = false, message = "Phòng không hợp lệ." });

            // Cập nhật giá và diện tích
            room.BasePrice = request.BasePrice;
            room.SurfaceArea = request.SurfaceArea;

            // Nếu chọn dùng giá riêng thì lưu, nếu dùng giá chung thì set Null
            if (request.UseBuildingUtilities)
            {
                room.ElectricityPrice = null;
                room.WaterPrice = null;
                room.InternetPrice = null;
                room.GarbagePrice = null;
            }
            else
            {
                room.ElectricityPrice = request.ElectricityPrice;
                room.WaterPrice = request.WaterPrice;
                room.InternetPrice = request.InternetPrice;
                room.GarbagePrice = request.GarbagePrice;
            }

            await context.SaveChangesAsync();
            return Json(new { success = true, message = "Lưu cài đặt phòng thành công!" });
        }
    }

    // Class phụ để hứng JSON gộp từ màn hình
    public class CreateBuildingRequest
    {
        public CreateBuildingDto Config { get; set; } = null!;
        public FloorSetupDto Structure { get; set; } = null!;
    }
    public class UpdateRoomConfigDto
    {
        public int RoomId { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? SurfaceArea { get; set; }
        public bool UseBuildingUtilities { get; set; } // Checkbox: Dùng giá chung hay giá riêng
        public decimal? ElectricityPrice { get; set; }
        public decimal? WaterPrice { get; set; }
        public decimal? InternetPrice { get; set; }
        public decimal? GarbagePrice { get; set; }
    }
}