using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantBillingController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICloudinaryService _cloudinaryService; // Khai báo Cloudinary

        // Inject CloudinaryService vào Constructor
        public TenantBillingController(IInvoiceService invoiceService, ICloudinaryService cloudinaryService)
        {
            _invoiceService = invoiceService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public async Task<IActionResult> MyInvoices([FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var invoices = await _invoiceService.GetTenantInvoicesAsync(tenantId);

            // Lấy danh sách tất cả phòng có hóa đơn của tenant (để làm filter dropdown)
            var rooms = await context.Contracts
                .Where(c => c.TenantId == tenantId)
                .Include(c => c.Room)
                .Select(c => new { c.Room.Id, c.Room.RoomNumber })
                .Distinct()
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            ViewBag.TenantRooms = rooms.Select(r => new { r.Id, r.RoomNumber }).ToList();
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> MyRoom([FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            
            var contracts = await context.Contracts
                .Where(c => c.TenantId == tenantId && c.Status == Domain.Enums.ContractStatus.Active)
                .Include(c => c.Room)
                    .ThenInclude(r => r.Floor)
                        .ThenInclude(f => f.Building)
                .Include(c => c.Owner)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View(contracts);
        }

        [HttpGet]
        public async Task<IActionResult> MyInvoicesByRoom(int roomId, string from = "myinvoices", [FromServices] Infrastructure.Persistence.ApplicationDbContext context = null)
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            var room = await context.Rooms
                .Include(r => r.Contracts.Where(c => c.TenantId == tenantId))
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Contracts.Any(c => c.TenantId == tenantId));

            if (room == null) return Forbid();

            var contractIds = room.Contracts.Select(c => c.Id).ToList();
            var invoices = await context.Invoices
                .Where(i => contractIds.Contains(i.ContractId))
                .Include(i => i.InvoiceItems)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            ViewBag.RoomNumber = room.RoomNumber;
            ViewBag.RoomId = roomId;
            ViewBag.FilteredByRoom = true;
            // Xác định URL nút Back dựa trên nguồn gốc truy cập
            ViewBag.BackUrl = from == "myroom"
                ? Url.Action("MyRoom", "TenantBilling")
                : Url.Action("MyInvoices", "TenantBilling");

            var viewModel = invoices.Select(i => new Application.DTOs.Billing.InvoiceListViewModel
            {
                InvoiceId = i.Id,
                RoomNumber = room.RoomNumber,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                Status = i.Status
            }).ToList();

            return View("MyInvoices", viewModel);
        }

        // Xóa action cũ MyInvoicesByContract vì đã thay bằng MyInvoicesByRoom


        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetail(int id)
        {
            try
            {
                var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var detail = await _invoiceService.GetInvoiceDetailAsync(id, tenantId);
                return Json(new { success = true, data = detail });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // [QUAN TRỌNG NHẤT]: Phải dùng [FromForm] cho cả 2 biến, tuyệt đối không còn chữ [FromBody] nào ở đây
        [HttpPost]
        public async Task<IActionResult> SubmitPayment([FromForm] int invoiceId, [FromForm] IFormFile? paymentProof)
        {
            try
            {
                var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                string proofUrl = "";

                // Xử lý Upload ảnh lên Cloudinary
                if (paymentProof != null)
                {
                    var uploadResult = await _cloudinaryService.UploadImageAsync(paymentProof, "payment_proofs");
                    proofUrl = uploadResult.Url; // Lấy link ảnh trả về
                }

                // Gọi Service để cập nhật trạng thái "Chờ duyệt" và lưu link ảnh
                await _invoiceService.SubmitPaymentProofAsync(invoiceId, tenantId, proofUrl);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Nếu Cloudinary lỗi mạng, nó sẽ nhảy vào đây và báo JSON "Lỗi:..." chứ không bị 500
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}