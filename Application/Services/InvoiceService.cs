using Application.DTOs.Billing;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using static Application.DTOs.Billing.InvoiceListViewModel;

namespace Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepo;

        public InvoiceService(IInvoiceRepository invoiceRepo)
        {
            _invoiceRepo = invoiceRepo;
        }

        public async Task<List<RoomUtilityInputViewModel>> GetRoomsForUtilityInputAsync(int buildingId, int month, int year, string landlordId)
        {
            var rooms = await _invoiceRepo.GetRoomsForUtilityInputAsync(buildingId, landlordId);
            var result = new List<RoomUtilityInputViewModel>();

            foreach (var room in rooms)
            {
                var activeContract = room.Contracts.FirstOrDefault();
                if (activeContract == null) continue;

                var building = room.Floor.Building;

                // [ĐÃ SỬA]: Tách số điện, nước từ List trả về
                var lastReadings = await _invoiceRepo.GetLastUtilityReadingsAsync(activeContract.Id);
                var lastElec = lastReadings.FirstOrDefault(r => r.UtilityType == UtilityType.Electricity);
                var lastWater = lastReadings.FirstOrDefault(r => r.UtilityType == UtilityType.Water);

                // Khúc này ở khoảng dòng 35
                result.Add(new RoomUtilityInputViewModel
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    ContractId = activeContract.Id,

                    // [BỔ SUNG HIỂN THỊ TÊN KHÁCH]: Ưu tiên lấy Tên, nếu không có thì lấy SĐT từ bảng User
                    TenantName = activeContract.Tenant?.FullName ?? activeContract.Tenant?.PhoneNumber ?? "Khách thuê",

                    OldElectricityIndex = lastElec?.NewIndex ?? 0,
                    OldWaterIndex = lastWater?.NewIndex ?? 0,
                    AppliedElectricityPrice = room.ElectricityPrice ?? building.ElectricityPrice,
                    AppliedWaterPrice = room.WaterPrice ?? building.WaterPrice,
                    AppliedInternetPrice = room.InternetPrice ?? building.InternetPrice,
                    AppliedGarbagePrice = room.GarbagePrice ?? building.GarbagePrice,

                    // [ĐÃ SỬA LỖI]: Lấy tiền thuê phòng từ Hợp Đồng thay vì giá BasePrice ảo
                    RoomRentPrice = activeContract.RentAmount
                });
            }
            return result.OrderBy(r => r.RoomNumber).ToList();
        }

        public async Task<bool> GenerateMonthlyInvoicesAsync(int buildingId, List<RecordUtilityDto> readings, string landlordId)
        {
            var rooms = await _invoiceRepo.GetRoomsForUtilityInputAsync(buildingId, landlordId);
            var newInvoices = new List<Invoice>();
            var newReadings = new List<UtilityReading>();

            foreach (var input in readings)
            {
                var room = rooms.FirstOrDefault(r => r.Id == input.RoomId);
                if (room == null) continue;

                var activeContract = room.Contracts.FirstOrDefault(c => c.Id == input.ContractId);
                if (activeContract == null) continue;

                var building = room.Floor.Building;

                // [ĐÃ SỬA]: Lấy chỉ số tháng trước
                var lastReadings = await _invoiceRepo.GetLastUtilityReadingsAsync(activeContract.Id);
                var lastElec = lastReadings.FirstOrDefault(r => r.UtilityType == UtilityType.Electricity);
                var lastWater = lastReadings.FirstOrDefault(r => r.UtilityType == UtilityType.Water);

                decimal oldElec = input.OldElectricityIndex;

                if (input.NewElectricityIndex < oldElec)
                {
                    throw new Exception($"Lỗi tại phòng {room.RoomNumber}: Số điện mới không được nhỏ hơn số cũ!");
                }

                // 1. Tính toán lượng tiêu thụ
                decimal elecConsumed = input.NewElectricityIndex - oldElec;
                decimal waterConsumed = input.WaterUsage;

                // 2. Chốt giá áp dụng
                decimal elecPrice = room.ElectricityPrice ?? building.ElectricityPrice;
                decimal waterPrice = room.WaterPrice ?? building.WaterPrice; // Lúc này nó mang ý nghĩa là Giá/Người
                decimal netPrice = room.InternetPrice ?? building.InternetPrice;
                decimal garbagePrice = room.GarbagePrice ?? building.GarbagePrice;
                decimal rentPrice = activeContract.RentAmount;

                // 3. Tính thành tiền
                decimal elecTotal = elecConsumed * elecPrice;
                decimal waterTotal = waterConsumed * waterPrice; // Số người x Giá
                decimal totalAmount = rentPrice + elecTotal + waterTotal + netPrice + garbagePrice;

                // 4. Tạo lịch sử chốt ĐIỆN
                newReadings.Add(new UtilityReading
                {
                    ContractId = activeContract.Id,
                    ReadingDate = new DateTime(input.Year, input.Month, 1),
                    UtilityType = UtilityType.Electricity,
                    OldIndex = oldElec,
                    NewIndex = input.NewElectricityIndex,
                    Usage = elecConsumed,
                    Amount = elecTotal
                });

                // [ĐÃ SỬA]: Khởi tạo Bản ghi chốt NƯỚC (Bỏ qua OldIndex và NewIndex)
                newReadings.Add(new UtilityReading
                {
                    ContractId = activeContract.Id,
                    ReadingDate = new DateTime(input.Year, input.Month, 1),
                    UtilityType = UtilityType.Water,
                    OldIndex = 0,
                    NewIndex = 0,
                    Usage = waterConsumed, // Lưu số người vào đây
                    Amount = waterTotal
                });

                // [ĐÃ SỬA]: Khởi tạo Hóa đơn dùng InvoiceDate thay vì IssueDate
                newInvoices.Add(new Invoice
                {
                    ContractId = activeContract.Id,
                    InvoiceDate = new DateTime(input.Year, input.Month, 1),
                    DueDate = new DateTime(input.Year, input.Month, 5),
                    TotalAmount = totalAmount,
                    Status = InvoiceStatus.Unpaid,
                    InvoiceItems = new List<InvoiceItem>
                    {
                        // [ĐÃ SỬA]: Bổ sung thêm biến ItemType cho tất cả các dòng
                        new InvoiceItem { ItemType = "Rent", Description = "Tiền thuê phòng", Amount = rentPrice },
                        new InvoiceItem { ItemType = "Electricity", Description = $"Tiền điện ({elecConsumed} kWh)", Amount = elecTotal },
                        new InvoiceItem { ItemType = "Water", Description = $"Tiền nước ({waterConsumed} khối)", Amount = waterTotal },
                        new InvoiceItem { ItemType = "Internet", Description = "Tiền Internet", Amount = netPrice },
                        new InvoiceItem { ItemType = "Garbage", Description = "Tiền rác", Amount = garbagePrice }
                    }
                });
            }

            var saveResult = await _invoiceRepo.SaveInvoicesAndReadingsAsync(newInvoices, newReadings);

            // Sau khi lưu thành công, gửi thông báo cho từng khách thuê
            if (saveResult)
            {
                var notifications = new List<Notification>();
                foreach (var inv in newInvoices)
                {
                    // Tìm hợp đồng tương ứng để lấy TenantId
                    var correspondingRoom = readings.FirstOrDefault(r => r.ContractId == inv.ContractId);
                    var room = rooms.FirstOrDefault(r => r.Contracts.Any(c => c.Id == inv.ContractId));
                    var contract = room?.Contracts.FirstOrDefault(c => c.Id == inv.ContractId);

                    if (contract?.TenantId == null) continue;

                    notifications.Add(new Notification
                    {
                        UserId = contract.TenantId,
                        Type = "NewInvoice",
                        Title = "Hóa đơn tiền thuê đã được gửi",
                        Content = $"Hóa đơn tiền thuê phòng {room.RoomNumber} tháng {inv.InvoiceDate.Month}/{inv.InvoiceDate.Year} đã sẵn sàng. Vui lòng vào trang Hóa đơn để xem chi tiết và thanh toán trước ngày {inv.DueDate:dd/MM/yyyy}.",
                        LinkedId = inv.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (notifications.Any())
                {
                    await _invoiceRepo.SaveNotificationsAsync(notifications);
                }
            }

            return saveResult;
        }
        // Cập nhật lại toàn bộ hàm GetInvoicesAsync
        public async Task<(List<InvoiceListViewModel> Items, int TotalPages, int CurrentPage)> GetInvoicesAsync(string landlordId, int? buildingId, int? month, int? year, InvoiceStatus? status = null, int pageIndex = 1, int pageSize = 10)
        {
            // 1. Lấy toàn bộ dữ liệu từ Database theo bộ lọc
            var invoices = await _invoiceRepo.GetInvoicesByLandlordAsync(landlordId, buildingId, month, year, status);

            // 2. Tính toán tổng số trang
            int totalCount = invoices.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // 3. Cắt dữ liệu bằng Skip và Take
            var pagedData = invoices.Skip((pageIndex - 1) * pageSize).Take(pageSize).Select(i => new InvoiceListViewModel
            {
                InvoiceId = i.Id,
                RoomNumber = i.Contract.Room.RoomNumber,
                TenantName = i.Contract.Tenant?.FullName ?? "Khách",
                TenantEmail = i.Contract.Tenant?.Email ?? "Không có",
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                PaymentProofPath = i.PaymentProofPath
            }).ToList();

            // 4. Trả về 3 tham số cùng lúc
            return (pagedData, totalPages, pageIndex);
        }

        public async Task<bool> MarkInvoiceAsPaidAsync(int invoiceId, string landlordId)
        {
            var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null || invoice.Contract.OwnerId != landlordId)
                throw new Exception("Hóa đơn không tồn tại hoặc bạn không có quyền!");

            // Nếu đã thanh toán rồi thì không làm gì cả
            if (invoice.Status == InvoiceStatus.Paid) return true;

            invoice.Status = InvoiceStatus.Paid;
            await _invoiceRepo.UpdateInvoiceAsync(invoice);

            return true;
        }
        public async Task<List<InvoiceListViewModel>> GetTenantInvoicesAsync(string tenantId)
        {
            var invoices = await _invoiceRepo.GetInvoicesByTenantAsync(tenantId);
            return invoices.Select(i => new InvoiceListViewModel
            {
                InvoiceId = i.Id,
                RoomNumber = i.Contract.Room.RoomNumber,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                Status = i.Status
            }).ToList();
        }

        public async Task<InvoiceDetailViewModel> GetInvoiceDetailAsync(int invoiceId, string tenantId)
        {
            var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null || invoice.Contract.TenantId != tenantId)
                throw new Exception("Hóa đơn không tồn tại hoặc không có quyền truy cập.");

            return new InvoiceDetailViewModel
            {
                InvoiceId = invoice.Id,
                Month = invoice.InvoiceDate.Month,
                Year = invoice.InvoiceDate.Year,
                RoomNumber = invoice.Contract.Room.RoomNumber,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                // Sẽ map dữ liệu ngân hàng thật từ Owner ở Phase 3
                OwnerAccountName = invoice.Contract.Owner.FullName?.ToUpper() ?? "NGUYEN VAN CHU NHA",
                Items = invoice.InvoiceItems.Select(item => new InvoiceItemViewModel
                {
                    ItemType = item.ItemType,
                    Description = item.Description,
                    Amount = item.Amount
                }).ToList()
            };
        }

        public async Task<bool> SubmitPaymentProofAsync(int invoiceId, string tenantId, string proofUrl)
        {
            var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null || invoice.Contract.TenantId != tenantId) throw new Exception("Lỗi bảo mật.");

            invoice.Status = InvoiceStatus.Pending; // Chuyển sang "Chờ duyệt"

            // [ĐÃ SỬA]: Lưu URL ảnh chụp màn hình vào Database
            if (!string.IsNullOrEmpty(proofUrl))
            {
                invoice.PaymentProofPath = proofUrl;
            }

            await _invoiceRepo.UpdateInvoiceAsync(invoice);
            return true;
        }
    }
}