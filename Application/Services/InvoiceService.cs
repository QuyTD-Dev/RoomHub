using Application.DTOs.Billing;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

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

                decimal oldElec = lastElec?.NewIndex ?? 0;

                if (input.NewElectricityIndex < oldElec)
                {
                    throw new Exception($"Lỗi tại phòng {room.RoomNumber}: Số điện mới không được nhỏ hơn số cũ!");
                }

                // 1. Tính toán lượng tiêu thụ
                decimal elecConsumed = input.NewElectricityIndex - oldElec;

                // [ĐÃ SỬA]: Tiêu thụ nước bây giờ chính là "Số lượng người" chủ nhà nhập
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

            return await _invoiceRepo.SaveInvoicesAndReadingsAsync(newInvoices, newReadings);
        }
    }
}