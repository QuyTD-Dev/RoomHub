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
        private readonly ICloudinaryService _cloudinaryService;
        public BuildingsController(IBuildingService buildingService, IBuildingRepository buildingRepository, ICloudinaryService cloudinaryService)
        {
            _buildingService = buildingService;
            _buildingRepository = buildingRepository;
            _cloudinaryService = cloudinaryService;
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
                if (request.Config.Photos != null && request.Config.Photos.Any())
                {
                    var file = request.Config.Photos.First();
                    var uploadResult = await _cloudinaryService.UploadImageAsync(file, "buildings");
                    request.Config.ThumbnailUrl = uploadResult.Url;
                }

                int buildingId = await _buildingService.CreateBuildingAsync(ownerId, request.Config, request.Structure);
                return Json(new { success = true, buildingId = buildingId, message = "Tạo tòa nhà thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var building = await _buildingRepository.GetBuildingDetailsAsync(id, ownerId);
            if (building == null) return NotFound();

            var dto = new UpdateBuildingDto
            {
                Id = building.Id,
                Name = building.Name,
                Province = building.City, // Note: using City as Province for consistency with Create
                District = building.District,
                Ward = building.Ward,
                StreetAddress = building.Address,
                ElectricityPrice = building.ElectricityPrice,
                WaterPrice = building.WaterPrice,
                InternetPrice = building.InternetPrice,
                GarbagePrice = building.GarbagePrice,
                ThumbnailUrl = building.ThumbnailUrl
            };
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> EditData([FromServices] ApplicationDbContext context, [FromForm] UpdateBuildingDto request)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var building = await context.Buildings.FirstOrDefaultAsync(b => b.Id == request.Id && b.OwnerId == ownerId);
                if (building == null) return Json(new { success = false, message = "Không tìm thấy tòa nhà hoặc bạn không có quyền sửa." });

                if (request.Photos != null && request.Photos.Any())
                {
                    var file = request.Photos.First();
                    var uploadResult = await _cloudinaryService.UploadImageAsync(file, "buildings");
                    building.ThumbnailUrl = uploadResult.Url;
                }

                building.Name = request.Name;
                building.Province = request.Province;
                building.City = request.Province;
                building.District = request.District;
                building.Ward = request.Ward;
                building.Address = request.StreetAddress;
                building.ElectricityPrice = request.ElectricityPrice;
                building.WaterPrice = request.WaterPrice;
                building.InternetPrice = request.InternetPrice;
                building.GarbagePrice = request.GarbagePrice;
                building.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
                return Json(new { success = true, buildingId = building.Id, message = "Cập nhật tòa nhà thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                bool success = await _buildingRepository.DeleteBuildingAsync(id, ownerId);
                
                if (success)
                    return Json(new { success = true, message = "Đã xóa tòa nhà thành công." });
                else
                    return Json(new { success = false, message = "Không tìm thấy tòa nhà hoặc bạn không có quyền thao tác." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi xóa tòa nhà. Vui lòng thử lại sau." });
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
        [HttpGet]
        public async Task<IActionResult> GetContractDetails([FromServices] ApplicationDbContext context, int roomId)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contract = await context.Contracts
                .Include(c => c.Tenant)
                .Where(c => c.RoomId == roomId && c.OwnerId == ownerId && c.Status == Domain.Enums.ContractStatus.Active)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            if (contract == null) return Json(new { success = false, message = "Không tìm thấy hợp đồng đang hoạt động cho phòng này." });

            return Json(new { 
                success = true, 
                data = new {
                    tenantName = contract.Tenant.FullName,
                    tenantPhone = contract.Tenant.PhoneNumber,
                    tenantEmail = contract.Tenant.Email,
                    startDate = contract.StartDate.ToString("dd/MM/yyyy"),
                    endDate = contract.EndDate.ToString("dd/MM/yyyy"),
                    rentAmount = contract.RentAmount,
                    depositAmount = contract.DepositAmount,
                    status = contract.Status.ToString()
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutRoom([FromServices] ApplicationDbContext context, [FromBody] CheckoutRoomRequest request)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId && r.LandlordId == ownerId);
                if (room == null) return Json(new { success = false, message = "Phòng không hợp lệ." });

                if (room.Status != Domain.Enums.RoomStatus.Occupied)
                    return Json(new { success = false, message = "Phòng chưa được cho thuê." });

                // Thanh lý hợp đồng Active hoặc Draft
                var contract = await context.Contracts
                    .Where(c => c.RoomId == room.Id && c.OwnerId == ownerId && (c.Status == Domain.Enums.ContractStatus.Active || c.Status == Domain.Enums.ContractStatus.Draft))
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (contract != null)
                {
                    contract.Status = Domain.Enums.ContractStatus.Liquidated;
                    contract.EndDate = DateTime.UtcNow.ToLocalTime();
                }

                room.Status = Domain.Enums.RoomStatus.Available; 
                
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã báo trả phòng thành công và thanh lý hợp đồng. Trạng thái phòng được chuyển về Còn trống." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi xử lý hệ thống: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckTenant(string email, [FromServices] UserManager<Domain.Entities.ApplicationUser> userManager)
        {
            var user = await userManager.FindByEmailAsync(email?.Trim());
            if (user != null)
            {
                return Json(new { exists = true, fullName = user.FullName, phone = user.PhoneNumber, email = user.Email });
            }
            return Json(new { exists = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetInvitationDetails(int roomId, [FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var contract = await context.Contracts
                .Include(c => c.Tenant)
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.RoomId == roomId && c.OwnerId == ownerId && c.Status == Domain.Enums.ContractStatus.Draft);

            if (contract == null) return Json(new { success = false, message = "Không tìm thấy lời mời." });

            return Json(new { 
                success = true, 
                data = new {
                    fullName = contract.Tenant?.FullName ?? "Khách vãng lai",
                    email = contract.Tenant?.Email,
                    phone = contract.Tenant?.PhoneNumber,
                    rentalPrice = contract.RentAmount,
                    depositAmount = contract.DepositAmount,
                    startDate = contract.StartDate.ToString("dd/MM/yyyy"),
                    endDate = contract.EndDate.ToString("dd/MM/yyyy")
                } 
            });
        }

        [HttpPost]
        public async Task<IActionResult> CancelInvitation(int roomId, [FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId && r.LandlordId == ownerId);
            if (room == null) return Json(new { success = false, message = "Phòng không hợp lệ." });

            var contract = await context.Contracts
                .FirstOrDefaultAsync(c => c.RoomId == roomId && c.OwnerId == ownerId && c.Status == Domain.Enums.ContractStatus.Draft);

            if (contract != null)
            {
                // Xóa chỉ số điện nước nháp
                var readings = await context.UtilityReadings.Where(u => u.ContractId == contract.Id).ToListAsync();
                if (readings.Any()) context.UtilityReadings.RemoveRange(readings);

                context.Contracts.Remove(contract);
            }

            room.Status = Domain.Enums.RoomStatus.Available;
            await context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy lời mời thành công." });
        }

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
                var room = await context.Rooms.Include(r => r.Floor).ThenInclude(f => f.Building).FirstOrDefaultAsync(r => r.Id == request.RoomId && r.LandlordId == ownerId);
                if (room == null) return Json(new { success = false, message = "Phòng không hợp lệ." });

                // 1. Kiểm tra Khách đã có tài khoản chưa dựa vào EMAIL (Khóa chính)
                var emailNormalized = request.Email?.Trim().ToLower();
                var tenantUser = await userManager.FindByEmailAsync(emailNormalized);
                bool isOnlineUser = tenantUser != null;

                if (!isOnlineUser)
                {
                    // Nếu khách chưa có tài khoản -> Tạo tài khoản Khách vãng lai (Guest)
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
                }

                // 2. Đổi trạng thái Phòng: Khách online -> PendingApproval (Màu vàng). Khách vãng lai -> Occupied (Xanh).
                room.Status = isOnlineUser ? Domain.Enums.RoomStatus.PendingApproval : Domain.Enums.RoomStatus.Occupied;

                // 3. Khởi tạo Hợp đồng (Contract)
                var contract = new Domain.Entities.Contract
                {
                    RoomId = room.Id,
                    TenantId = tenantUser.Id,
                    OwnerId = ownerId!,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    RentAmount = request.RentalPrice,
                    DepositAmount = request.DepositAmount,
                    // [QUAN TRỌNG]: Khách dùng web -> Draft (Chờ xác nhận). Khách vãng lai -> Active luôn.
                    Status = isOnlineUser ? Domain.Enums.ContractStatus.Draft : Domain.Enums.ContractStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.Contracts.Add(contract);
                await context.SaveChangesAsync(); // Cần lưu trước để lấy ContractId

                // Nếu là khách dùng web, tạo Notification để báo họ xác nhận
                if (isOnlineUser)
                {
                    var notification = new Domain.Entities.Notification
                    {
                        UserId = tenantUser.Id,
                        Type = "ContractAssign",
                        Title = "Mời nhận phòng",
                        Content = $"Chủ nhà đã thêm bạn vào Phòng P.{room.RoomNumber}, Tòa nhà {room.Floor?.Building?.Name ?? "của họ"}. Nhấn vào đây để xem và xác nhận.",
                        LinkedId = contract.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Notifications.Add(notification);
                }

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

                
                context.UtilityReadings.Add(elecReading);

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
        public DateTime EndDate { get; set; }
        public decimal InitialElec { get; set; }
    }
    public class CheckoutRoomRequest
    {
        public int RoomId { get; set; }
    }
    public class UpdateBuildingDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string District { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string StreetAddress { get; set; } = null!;
        public decimal ElectricityPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal InternetPrice { get; set; }
        public decimal GarbagePrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }
}