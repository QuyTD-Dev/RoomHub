using Domain.Enums;

namespace Application.DTOs.Admin
{
    public class RoomListAdminViewModel
    {
        public List<RoomItemAdminViewModel> Rooms { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public RoomStatus? CurrentStatus { get; set; }
        public string? CurrentSearch { get; set; }

        // AI
        public Dictionary<int, Application.Interfaces.Services.PriceSuggestionResult> PriceSuggestions { get; set; } = new();
    }

    public class RoomItemAdminViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public string BuildingName { get; set; } = null!;
        public int FloorNumber { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? SurfaceArea { get; set; }
        public RoomStatus Status { get; set; }
        public string LandlordName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
