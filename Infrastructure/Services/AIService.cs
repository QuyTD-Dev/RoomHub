using Application.DTOs.Admin;
using Application.Interfaces.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIService> _logger;

        public AIService(
            HttpClient httpClient,
            IOptions<GeminiSettings> settings,
            ApplicationDbContext context,
            ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _context = context;
            _logger = logger;
        }

        // ══════════════════════════════════════════════
        // 1. DASHBOARD INSIGHTS
        // ══════════════════════════════════════════════
        public async Task<string> GetDashboardInsightsAsync(DashboardViewModel data)
        {
            var prompt = $@"Bạn là AI assistant cho hệ thống quản lý phòng trọ RoomHub. Phân tích dữ liệu sau và đưa ra 3-4 nhận xét ngắn gọn (mỗi nhận xét 1 dòng, bắt đầu bằng emoji phù hợp):

Dữ liệu hiện tại:
- Tổng phòng: {data.TotalRooms}
- Hợp đồng đang hoạt động: {data.ActiveContracts}
- Tổng doanh thu: {data.TotalRevenue:N0} VND
- Ticket bảo trì đang mở: {data.OpenTickets}
- Tổng người dùng: {data.TotalUsers}
- Tổng tòa nhà: {data.TotalBuildings}
- Tỷ lệ thuê phòng: {data.OccupancyRate}%

Hãy phân tích xu hướng, đưa ra cảnh báo nếu có vấn đề, và gợi ý cải thiện. Trả lời bằng tiếng Việt, ngắn gọn và chuyên nghiệp.";

            return await CallGeminiAsync(prompt) ?? "Không thể phân tích dữ liệu lúc này.";
        }

        // ══════════════════════════════════════════════
        // 2. USER RISK ANALYSIS
        // ══════════════════════════════════════════════
        public async Task<Dictionary<string, UserRiskResult>> GetUserRiskAnalysisAsync(List<UserItemViewModel> users)
        {
            var results = new Dictionary<string, UserRiskResult>();

            // Rule-based risk scoring (không cần gọi API cho mỗi user)
            foreach (var user in users)
            {
                var riskScore = 0;
                var reasons = new List<string>();

                // Chưa xác thực
                if (!user.IsVerified)
                {
                    riskScore += 2;
                    reasons.Add("Chưa xác thực danh tính");
                }

                // Đã bị ban
                if (user.IsBanned)
                {
                    riskScore += 5;
                    reasons.Add("Tài khoản đã bị cấm");
                }

                // Tài khoản mới (< 7 ngày)
                if ((DateTime.UtcNow - user.CreatedAt).TotalDays < 7)
                {
                    riskScore += 1;
                    reasons.Add("Tài khoản mới tạo");
                }

                // Không có email
                if (string.IsNullOrEmpty(user.Email))
                {
                    riskScore += 2;
                    reasons.Add("Thiếu email");
                }

                // Không có SĐT
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    riskScore += 1;
                    reasons.Add("Thiếu số điện thoại");
                }

                var level = riskScore switch
                {
                    >= 5 => "High",
                    >= 3 => "Medium",
                    _ => "Low"
                };

                results[user.Id] = new UserRiskResult
                {
                    Level = level,
                    Reason = reasons.Any() ? string.Join(", ", reasons) : "Hồ sơ hoàn chỉnh"
                };
            }

            // Bổ sung: Gọi Gemini phân tích tổng quan danh sách users
            try
            {
                var highRiskCount = results.Count(r => r.Value.Level == "High");
                var mediumRiskCount = results.Count(r => r.Value.Level == "Medium");
                if (highRiskCount > 0 || mediumRiskCount > 0)
                {
                    _logger.LogInformation("AI Risk Analysis: {High} high-risk, {Medium} medium-risk users detected",
                        highRiskCount, mediumRiskCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI risk logging failed");
            }

            return results;
        }

        // ══════════════════════════════════════════════
        // 3. ROOM PRICE SUGGESTION
        // ══════════════════════════════════════════════
        public async Task<Dictionary<int, PriceSuggestionResult>> GetPriceSuggestionsAsync(List<RoomItemAdminViewModel> rooms)
        {
            var results = new Dictionary<int, PriceSuggestionResult>();

            if (!rooms.Any()) return results;

            // Tính trung bình giá theo khu vực (BuildingName)
            var avgByArea = rooms
                .GroupBy(r => r.BuildingName)
                .ToDictionary(g => g.Key, g => g.Average(r => r.BasePrice));

            var overallAvg = rooms.Average(r => r.BasePrice);

            // Build prompt cho Gemini
            var roomData = rooms.Select(r => new
            {
                r.Id,
                r.Title,
                r.BasePrice,
                r.BuildingName,
                Area = r.SurfaceArea,
                r.Status
            });

            var prompt = $@"Bạn là AI pricing advisor cho hệ thống phòng trọ. Phân tích danh sách phòng và đề xuất giá hợp lý.

Dữ liệu phòng (JSON):
{JsonSerializer.Serialize(roomData, new JsonSerializerOptions { WriteIndented = true })}

Giá trung bình toàn hệ thống: {overallAvg:N0} VND

Trả về JSON array với format CHÍNH XÁC sau (không có markdown, không có giải thích):
[{{""id"": 1, ""suggestedPrice"": 3500000, ""trend"": ""up"", ""note"": ""Giá thấp hơn khu vực 15%""}}]

Trong đó trend: ""up"" (nên tăng), ""down"" (nên giảm), ""stable"" (hợp lý).
Chỉ trả JSON, không có text khác.";

            try
            {
                var response = await CallGeminiAsync(prompt);
                if (!string.IsNullOrEmpty(response))
                {
                    // Clean response - remove markdown fences if any
                    response = response.Trim();
                    if (response.StartsWith("```"))
                    {
                        response = response.Split('\n').Skip(1).Take(response.Split('\n').Length - 2)
                            .Aggregate("", (a, b) => a + b);
                    }
                    if (response.StartsWith("```json"))
                    {
                        response = response.Replace("```json", "").Replace("```", "").Trim();
                    }

                    var suggestions = JsonSerializer.Deserialize<List<PriceSuggestionJson>>(response,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (suggestions != null)
                    {
                        foreach (var s in suggestions)
                        {
                            results[s.Id] = new PriceSuggestionResult
                            {
                                SuggestedPrice = s.SuggestedPrice,
                                Trend = s.Trend ?? "stable",
                                Note = s.Note ?? ""
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI price suggestion parsing failed, using fallback");
            }

            // Fallback cho phòng chưa có AI suggestion
            foreach (var room in rooms)
            {
                if (!results.ContainsKey(room.Id))
                {
                    var areaAvg = avgByArea.GetValueOrDefault(room.BuildingName, overallAvg);
                    var diff = room.BasePrice - areaAvg;
                    var pctDiff = areaAvg > 0 ? (diff / areaAvg) * 100 : 0;

                    results[room.Id] = new PriceSuggestionResult
                    {
                        SuggestedPrice = Math.Round(areaAvg / 100000) * 100000, // Round to 100k
                        Trend = pctDiff > 10 ? "down" : pctDiff < -10 ? "up" : "stable",
                        Note = pctDiff > 10 ? $"Cao hơn khu vực {pctDiff:F0}%"
                             : pctDiff < -10 ? $"Thấp hơn khu vực {Math.Abs(pctDiff):F0}%"
                             : "Giá phù hợp thị trường"
                    };
                }
            }

            return results;
        }

        // ══════════════════════════════════════════════
        // 4. REVIEW SENTIMENT SUMMARY
        // ══════════════════════════════════════════════
        public async Task<SentimentSummaryResult> GetReviewSentimentSummaryAsync()
        {
            var result = new SentimentSummaryResult();

            try
            {
                var recentReviews = await _context.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(50)
                    .Select(r => new { r.Comment, r.Rating })
                    .ToListAsync();

                if (!recentReviews.Any())
                {
                    result.Summary = "Chưa có đánh giá nào trong hệ thống.";
                    return result;
                }

                // Quick rating-based classification
                result.Positive = recentReviews.Count(r => r.Rating >= 4);
                result.Neutral = recentReviews.Count(r => r.Rating == 3);
                result.Negative = recentReviews.Count(r => r.Rating <= 2);

                // AI sentiment analysis from comments
                var comments = recentReviews
                    .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                    .Select(r => r.Comment)
                    .Take(20)
                    .ToList();

                if (comments.Any())
                {
                    var prompt = $@"Phân tích cảm xúc tổng hợp từ các đánh giá phòng trọ sau. Đưa ra nhận xét ngắn gọn 2-3 câu bằng tiếng Việt về xu hướng chung:

{string.Join("\n", comments.Select((c, i) => $"{i + 1}. {c}"))}

Trả lời ngắn gọn, chuyên nghiệp.";

                    var aiSummary = await CallGeminiAsync(prompt);
                    result.Summary = aiSummary ?? "Không thể phân tích sentiment lúc này.";
                }
                else
                {
                    result.Summary = $"Có {recentReviews.Count} đánh giá: {result.Positive} tích cực, {result.Neutral} trung lập, {result.Negative} tiêu cực.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sentiment analysis failed");
                result.Summary = "Không thể phân tích sentiment lúc này.";
            }

            return result;
        }

        // ══════════════════════════════════════════════
        // GEMINI API CALL
        // ══════════════════════════════════════════════
        private async Task<string?> CallGeminiAsync(string prompt)
        {
            try
            {
                var model = string.IsNullOrEmpty(_settings.Model) ? "gemini-2.0-flash" : _settings.Model;
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_settings.ApiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1024,
                        topP = 0.9
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API error {StatusCode}: {Response}",
                        response.StatusCode, responseJson);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call failed");
                return null;
            }
        }

        // Helper for JSON parsing
        private class PriceSuggestionJson
        {
            public int Id { get; set; }
            public decimal SuggestedPrice { get; set; }
            public string? Trend { get; set; }
            public string? Note { get; set; }
        }
    }
}
