namespace Application.DTOs.Buildings
{
    public class FloorSetupDto
    {
        public int NumberOfFloors { get; set; }
        public int RoomsPerFloor { get; set; }

        // Dành cho trường hợp chủ nhà muốn tùy chỉnh số phòng từng tầng ở giao diện bên trái
        public List<FloorDetailDto> CustomFloors { get; set; } = new List<FloorDetailDto>();
    }

    public class FloorDetailDto
    {
        public int FloorNumber { get; set; }
        public int NumberOfRooms { get; set; }
    }
}