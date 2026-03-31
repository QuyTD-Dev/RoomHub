namespace Application.DTOs.Buildings
{
    public class BuildingListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;

        // Hiển thị nhanh số lượng phòng để chủ nhà theo dõi
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }

        public string? ThumbnailUrl { get; set; }
    }
}