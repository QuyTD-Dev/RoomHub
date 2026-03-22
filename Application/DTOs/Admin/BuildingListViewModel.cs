namespace Application.DTOs.Admin
{
    public class BuildingListViewModel
    {
        public List<BuildingItemViewModel> Buildings { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public string? CurrentSearch { get; set; }
    }

    public class BuildingItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string OwnerName { get; set; } = null!;
        public int FloorCount { get; set; }
        public int RoomCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
