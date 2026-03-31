using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.DTOs.RoomPosts
{
    public class CreateRoomViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng để đăng tin")]
        public int SelectedRoomId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        public string Description { get; set; } = null!;

        public List<IFormFile>? Photos { get; set; }

        // List để chứa danh sách phòng đổ ra Dropdown
        public IEnumerable<Room> AvailableRooms { get; set; } = new List<Room>();
    }
}