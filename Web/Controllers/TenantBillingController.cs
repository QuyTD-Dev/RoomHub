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
        public async Task<IActionResult> SubmitPayment([FromForm] int invoiceId, [FromForm] IFormFile? paymentProof,
            [FromServices] Infrastructure.Persistence.ApplicationDbContext context)
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

                // Gửi thông báo cho chủ nhà
                var invoice = await context.Invoices
                    .Include(i => i.Contract)
                        .ThenInclude(c => c.Room)
                    .Include(i => i.Contract.Tenant)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice != null)
                {
                    var tenantName = invoice.Contract.Tenant?.FullName ?? User.Identity!.Name ?? "Khách thuê";
                    context.Notifications.Add(new Domain.Entities.Notification
                    {
                        UserId = invoice.Contract.OwnerId,
                        Type = "PaymentSubmitted",
                        Title = "Khách đã chuyển khoản",
                        Content = $"{tenantName} đã gửi biên lai thanh toán hóa đơn Phòng {invoice.Contract.Room.RoomNumber} tháng {invoice.InvoiceDate.Month}/{invoice.InvoiceDate.Year}. Vui lòng kiểm tra và xác nhận.",
                        LinkedId = invoiceId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Nếu Cloudinary lỗi mạng, nó sẽ nhảy vào đây và báo JSON "Lỗi:..." chứ không bị 500
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingContracts([FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var pending = await context.Contracts
                .Include(c => c.Room)
                .ThenInclude(r => r.Floor)
                .ThenInclude(f => f.Building)
                .Include(c => c.Owner)
                .Where(c => c.TenantId == tenantId && c.Status == Domain.Enums.ContractStatus.Draft)
                .Select(c => new {
                    contractId = c.Id,
                    roomNumber = c.Room.RoomNumber,
                    buildingName = c.Room.Floor.Building.Name,
                    ownerName = c.Owner.FullName,
                    rent = c.RentAmount,
                    date = c.StartDate.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new { count = pending.Count, data = pending });
        }

        [HttpPost]
        public async Task<IActionResult> RespondToContract(int contractId, bool accept, [FromServices] Infrastructure.Persistence.ApplicationDbContext context)
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var contract = await context.Contracts
                .Include(c => c.Room)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == contractId && c.TenantId == tenantId && c.Status == Domain.Enums.ContractStatus.Draft);

            if (contract == null) return Json(new { success = false, message = "Không tìm thấy lời mời nhận phòng." });

            if (accept)
            {
                contract.Status = Domain.Enums.ContractStatus.Active;
                contract.Room.Status = Domain.Enums.RoomStatus.Occupied; // CẬP NHẬT: Chuyển sang Đang Ở
                
                // Gửi thông báo lại cho chủ nhà
                var notif = new Domain.Entities.Notification
                {
                    UserId = contract.OwnerId,
                    Type = "ContractResponse",
                    Title = "Khách đã nhận phòng",
                    Content = $"Khách thuê {User.Identity.Name} đã CHẤP NHẬN vào ở Phòng P.{contract.Room.RoomNumber}.",
                    LinkedId = contract.RoomId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.Notifications.Add(notif);
            }
            else
            {
                // Từ chối -> Xóa cứng hợp đồng nháp & Đổi trạng thái phòng lại Available (CẬP NHẬT)
                contract.Room.Status = Domain.Enums.RoomStatus.Available;
                
                var notif = new Domain.Entities.Notification
                {
                    UserId = contract.OwnerId,
                    Type = "ContractResponse",
                    Title = "Khách từ chối phòng",
                    Content = $"Khách thuê {User.Identity.Name} đã TỪ CHỐI lời mời vào Phòng P.{contract.Room.RoomNumber}.",
                    LinkedId = contract.RoomId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.Notifications.Add(notif);
                
                // Xóa cả hóa đơn điện nước nháp nếu có
                var readings = await context.UtilityReadings.Where(u => u.ContractId == contract.Id).ToListAsync();
                if(readings.Any()) context.UtilityReadings.RemoveRange(readings);

                context.Contracts.Remove(contract); // Xóa cứng Draft để dọn rác DB
            }

            await context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}