using Domain.Enums;

namespace Application.DTOs.Billing
{
    public class InvoiceListViewModel
    {
        public int InvoiceId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public string TenantName { get; set; } = null!;
        public string TenantEmail { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public string? PaymentProofPath { get; set; } // Ảnh bill chuyển khoản (dành cho Phase sau)
        public class InvoiceDetailViewModel
        {
            public int InvoiceId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public string RoomNumber { get; set; } = null!;
            public decimal TotalAmount { get; set; }
            public Domain.Enums.InvoiceStatus Status { get; set; }

            // Thông tin Ngân hàng của Chủ nhà (Mặc định tạm thời, Phase 3 sẽ lấy từ DB)
            public string OwnerBankName { get; set; } = "MBBank";
            public string OwnerBankAccount { get; set; } = "0987654321";
            public string OwnerAccountName { get; set; } = "CHỦ NHÀ";

            public List<InvoiceItemViewModel> Items { get; set; } = new List<InvoiceItemViewModel>();
        }

        public class InvoiceItemViewModel
        {
            public string ItemType { get; set; } = null!;
            public string? Description { get; set; }
            public decimal Amount { get; set; }
        }
    }
}