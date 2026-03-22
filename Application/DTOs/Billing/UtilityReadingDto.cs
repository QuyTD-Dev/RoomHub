namespace Application.DTOs.Billing
{
    // Dùng để hứng dữ liệu từ giao diện lưới chốt số của Chủ nhà truyền xuống
    public class RecordUtilityDto
    {
        public int RoomId { get; set; }
        public int ContractId { get; set; }
        public decimal NewElectricityIndex { get; set; }

        // [ĐÃ SỬA]: Thay vì NewWaterIndex, ta dùng WaterUsage để biểu diễn "Số người"
        public decimal WaterUsage { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }
    }

    // Dùng để hiển thị lên lưới nhập liệu cho chủ nhà
    public class RoomUtilityInputViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public int ContractId { get; set; }
        public string TenantName { get; set; } = null!;

        // Chỉ số tháng trước (Để đối chiếu)
        public decimal OldElectricityIndex { get; set; }
        public decimal OldWaterIndex { get; set; }

        // Giá áp dụng (Ưu tiên giá phòng, nếu null lấy giá tòa nhà)
        public decimal AppliedElectricityPrice { get; set; }
        public decimal AppliedWaterPrice { get; set; }
        public decimal AppliedInternetPrice { get; set; }
        public decimal AppliedGarbagePrice { get; set; }
        public decimal RoomRentPrice { get; set; }
    }
}