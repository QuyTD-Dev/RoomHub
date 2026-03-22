namespace Application.DTOs.Admin
{
    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public string? CurrentRole { get; set; }
        public string? CurrentSearch { get; set; }

        // AI
        public Dictionary<string, Application.Interfaces.Services.UserRiskResult> UserRisks { get; set; } = new();
    }

    public class UserItemViewModel
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = null!;
        public bool IsVerified { get; set; }
        public bool IsBanned { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
