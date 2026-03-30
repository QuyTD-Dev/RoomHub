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
        public async Task<IActionResult> MyInvoices()
        {
            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var invoices = await _invoiceService.GetTenantInvoicesAsync(tenantId);
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