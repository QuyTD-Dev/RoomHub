using Application.DTOs.Buildings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [HttpPost]
        public async Task<IActionResult> CreateData([FromBody] CreateBuildingRequest request)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                // Gọi Service để thực hiện thuật toán tạo Tầng & Phòng
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
    }

    // Class phụ để hứng JSON gộp từ màn hình
    public class CreateBuildingRequest
    {
        public CreateBuildingDto Config { get; set; } = null!;
        public FloorSetupDto Structure { get; set; } = null!;
    }
}