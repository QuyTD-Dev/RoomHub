using Domain.Entities;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.RoomPosts
{
    public class CreateRoomViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Room Number is required")]
        [StringLength(50)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = null!;

        [Required(ErrorMessage = "Room Type is required")]
        [Display(Name = "Room Type")]
        public RoomType RoomType { get; set; }


        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        [Display(Name = "Base Price")]
        public decimal BasePrice { get; set; }

        [Range(0, 1000, ErrorMessage = "Area must be between 0 and 1000")]
        [Display(Name = "Surface Area (m2)")]
        public decimal? SurfaceArea { get; set; }

        [Display(Name = "Max Capacity")]
        [Range(1, 20)]
        public int MaxCapacity { get; set; } = 2;

        public string? Description { get; set; }

        [Display(Name = "Is Furnished")]
        public bool IsFurnished { get; set; } = true;

        [Required(ErrorMessage = "Floor is required")]
        [Display(Name = "Floor")]
        public int FloorId { get; set; }

        public IEnumerable<Floor> AvailableFloors { get; set; } = new List<Floor>();

        [Display(Name = "Status")]
        public RoomStatus Status { get; set; } = RoomStatus.Active;

        public List<int> SelectedAmenityIds { get; set; } = new List<int>();
        
        public List<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();
    }
}
