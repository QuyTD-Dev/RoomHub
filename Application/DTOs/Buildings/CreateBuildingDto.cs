using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Buildings
{
    public class CreateBuildingDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tòa nhà")]
        public string Name { get; set; } = null!;

        [Required]
        public string BuildingType { get; set; } = null!; // Chung cư mini, Dãy trọ...

        [Required]
        public string Province { get; set; } = null!;
        [Required]
        public string District { get; set; } = null!;
        [Required]
        public string Ward { get; set; } = null!;
        [Required]
        public string StreetAddress { get; set; } = null!;

        public string? Description { get; set; }

        // Dịch vụ & Tính phí chung cho toàn bộ tòa nhà
        public decimal ElectricityPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal InternetPrice { get; set; }
        public decimal GarbagePrice { get; set; }

        public string? Amenities { get; set; }
        public string? Rules { get; set; }

        // Hình ảnh tổng quan của tòa nhà
        public List<IFormFile>? Photos { get; set; }
    }
}