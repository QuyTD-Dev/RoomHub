using Application.DTOs.Admin;

namespace Application.Interfaces.Services
{
    public interface IAIService
    {
        /// <summary>
        /// AI phân tích dữ liệu dashboard → đưa ra nhận xét, xu hướng, cảnh báo
        /// </summary>
        Task<string> GetDashboardInsightsAsync(DashboardViewModel dashboardData);

        /// <summary>
        /// AI phát hiện user rủi ro → trả về dictionary userId → risk level (Low/Medium/High) + reason
        /// </summary>
        Task<Dictionary<string, UserRiskResult>> GetUserRiskAnalysisAsync(List<UserItemViewModel> users);

        /// <summary>
        /// AI so sánh giá phòng → gợi ý giá hợp lý cho từng phòng
        /// </summary>
        Task<Dictionary<int, PriceSuggestionResult>> GetPriceSuggestionsAsync(List<RoomItemAdminViewModel> rooms);

        /// <summary>
        /// AI phân tích sentiment tổng hợp từ reviews
        /// </summary>
        Task<SentimentSummaryResult> GetReviewSentimentSummaryAsync();
    }

    public class UserRiskResult
    {
        public string Level { get; set; } = "Low"; // Low, Medium, High
        public string Reason { get; set; } = string.Empty;
    }

    public class PriceSuggestionResult
    {
        public decimal SuggestedPrice { get; set; }
        public string Trend { get; set; } = "stable"; // up, down, stable
        public string Note { get; set; } = string.Empty;
    }

    public class SentimentSummaryResult
    {
        public int Positive { get; set; }
        public int Neutral { get; set; }
        public int Negative { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}
