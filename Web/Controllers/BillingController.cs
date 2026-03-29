using Application.DTOs.Billing;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "PropertyOwner")]
    public class BillingController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IBuildingService _buildingService;

        public BillingController(IInvoiceService invoiceService, IBuildingService buildingService)
        {
            _invoiceService = invoiceService;
            _buildingService = buildingService;
        }

        // 1. MÀN HÌNH HIỂN THỊ LƯỚI CHỐT SỐ
        [HttpGet]
        public async Task<IActionResult> Index(int? buildingId, int? month, int? year)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            // Lấy danh sách tòa nhà đổ ra Dropdown
            ViewBag.Buildings = await _buildingService.GetMyBuildingsAsync(ownerId);

            // Mặc định là tháng/năm hiện tại nếu chưa chọn
            int currentMonth = month ?? DateTime.Now.Month;
            int currentYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedBuildingId = buildingId;
            ViewBag.SelectedMonth = currentMonth;
            ViewBag.SelectedYear = currentYear;

            var rooms = new List<RoomUtilityInputViewModel>();

            // Nếu có chọn tòa nhà, gọi Service lấy danh sách phòng và chỉ số cũ
            if (buildingId.HasValue)
            {
                rooms = await _invoiceService.GetRoomsForUtilityInputAsync(buildingId.Value, currentMonth, currentYear, ownerId);
            }

            return View(rooms);
        }

        // 2. API NHẬN DỮ LIỆU TỪ LƯỚI BẮN LÊN ĐỂ TẠO HÓA ĐƠN
        [HttpPost]
        public async Task<IActionResult> GenerateInvoices([FromBody] SaveReadingsRequest request)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            try
            {
                if (request.Readings == null || !request.Readings.Any())
                    return Json(new { success = false, message = "Không có dữ liệu để chốt!" });

                var result = await _invoiceService.GenerateMonthlyInvoicesAsync(request.BuildingId, request.Readings, ownerId);
                return Json(new { success = true, message = "Chốt số và xuất hóa đơn thành công!" });
            }
            catch (Exception ex)
            {
                // [ĐÃ SỬA]: Móc InnerException để hiển thị lỗi gốc rễ từ SQL Server
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = detailedError });
            }
        }
        // MÀN HÌNH DANH SÁCH HÓA ĐƠN
        [HttpGet]
        public async Task<IActionResult> Invoices(int? buildingId, int? month, int? year, Domain.Enums.InvoiceStatus? status, int page = 1)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            // [ĐÃ SỬA]: Khai báo biến cục bộ rõ ràng kiểu 'int' để tránh bị 'dynamic'
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            ViewBag.Buildings = await _buildingService.GetMyBuildingsAsync(ownerId);
            ViewBag.SelectedBuildingId = buildingId;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedStatus = status;

            // [ĐÃ SỬA]: Truyền biến kiểu int vào Service thay vì truyền ViewBag
            var result = await _invoiceService.GetInvoicesAsync(ownerId, buildingId, selectedMonth, selectedYear, status, page, 10);

            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;

            return View(result.Items);
        }

        // API XÁC NHẬN ĐÃ THU TIỀN
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid([FromBody] int invoiceId)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            try
            {
                await _invoiceService.MarkInvoiceAsPaidAsync(invoiceId, ownerId);
                return Json(new { success = true, message = "Đã gạch nợ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Class hứng dữ liệu JSON
    public class SaveReadingsRequest
    {
        public int BuildingId { get; set; }
        public List<RecordUtilityDto> Readings { get; set; } = new List<RecordUtilityDto>();
    }
}