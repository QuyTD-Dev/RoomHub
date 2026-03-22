namespace Application.DTOs.Admin
{
    public class DashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int ActiveContracts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OpenTickets { get; set; }
        public decimal OccupancyRate { get; set; }
        public int TotalUsers { get; set; }
        public int TotalBuildings { get; set; }
        public List<RevenueDataPoint> RevenueLast30Days { get; set; } = new();
        public List<TopRoomViewModel> TopViewedRooms { get; set; } = new();
        public List<RecentUserViewModel> RecentUsers { get; set; } = new();

        // AI
        public string? AiInsights { get; set; }
        public string? SentimentSummary { get; set; }
        public int SentimentPositive { get; set; }
        public int SentimentNeutral { get; set; }
        public int SentimentNegative { get; set; }
    }

    public class RevenueDataPoint
    {
        public string Date { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class TopRoomViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Address { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = null!;
        public int ReviewCount { get; set; }
    }

    public class RecentUserViewModel
    {
        public string Id { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Email { get; set; }
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
