using Application.DTOs.Buildings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> AddDirectTenant(
            [FromServices] Infrastructure.Persistence.ApplicationDbContext context,
            [FromServices] UserManager<Domain.Entities.ApplicationUser> userManager,
            [FromBody] AddTenantDto request)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId && r.LandlordId == ownerId);
                if (room == null) return Json(new { success = false, message = "Phòng không hợp lệ." });

                // 1. Kiểm tra Khách đã có tài khoản chưa dựa vào SĐT. Nếu chưa -> Tạo tài khoản Khách (Guest)
                // 1. Kiểm tra Khách đã có tài khoản chưa dựa vào EMAIL (Khóa chính)
                var tenantUser = await userManager.FindByEmailAsync(request.Email);
                if (tenantUser == null)
                {
                    tenantUser = new Domain.Entities.ApplicationUser
                    {
                        UserName = request.Email, // Bắt buộc dùng Email làm UserName
                        Email = request.Email,
                        PhoneNumber = request.PhoneNumber, // Vẫn lưu SĐT để chủ nhà liên hệ
                        FullName = request.FullName,
                        AvatarUrl = "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(request.FullName)
                    };

                    var result = await userManager.CreateAsync(tenantUser, "Guest@123456A!");
                    if (!result.Succeeded)
                    {
                        string identityErrors = string.Join(", ", result.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = "Lỗi tạo tài khoản: " + identityErrors });
                    }

                    // (Tùy chọn) Gán luôn quyền Tenant cho khách này để họ có thể đăng nhập xài App
                    // await userManager.AddToRoleAsync(tenantUser, "Tenant");
                }

                // 2. Đổi trạng thái Phòng thành Đang Thuê
                room.Status = Domain.Enums.RoomStatus.Occupied;

                // 3. Khởi tạo Hợp đồng (Contract)
                var contract = new Domain.Entities.Contract
                {
                    RoomId = room.Id,
                    TenantId = tenantUser.Id,
                    OwnerId = ownerId!,
                    StartDate = request.StartDate,
                    EndDate = request.StartDate.AddMonths(6),
                    RentAmount = request.RentalPrice,
                    DepositAmount = request.DepositAmount,
                    Status = Domain.Enums.ContractStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.Contracts.Add(contract);
                await context.SaveChangesAsync(); // Cần lưu trước để lấy ContractId

                // 4. Chốt số Điện Nước đầu kỳ
                var elecReading = new Domain.Entities.UtilityReading
                {
                    ContractId = contract.Id,
                    UtilityType = Domain.Enums.UtilityType.Electricity,
                    ReadingDate = request.StartDate,
                    NewIndex = request.InitialElec,
                    OldIndex = 0,
                    Usage = 0,
                    Amount = 0
                };

                
                context.UtilityReadings.AddRange(elecReading);

                // 5. Hoàn tất toàn bộ
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thêm khách và chốt số đầu kỳ thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // [NÂNG CẤP QUAN TRỌNG]: Lấy lỗi chi tiết từ InnerException
                // Nhờ dòng này, nếu DB còn thiếu trường gì, trình duyệt sẽ alert rõ ràng (Ví dụ: "Cannot insert the value NULL into column 'FirstName'")
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return Json(new { success = false, message = detailedError });
            }
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
    public class AddTenantDto
    {
        public int RoomId { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? IdentityCard { get; set; }
        public decimal RentalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public DateTime StartDate { get; set; }
        public decimal InitialElec { get; set; }
    }
}