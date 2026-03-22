using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly IRoomPostService _roomService;

        public ChatbotService(HttpClient httpClient, IConfiguration configuration, IRoomPostService roomService)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"];
            _roomService = roomService;
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return "Xin lỗi, hiện tại Robot hỗ trợ đang gặp lỗi cấu hình.";

            try
            {
                // 1. Lấy dữ liệu phòng từ Database để làm ngữ cảnh
                var rooms = await _roomService.GetAllRoomsAsync();
                var roomContext = string.Join("\n", rooms.Items.Select(r => 
                    $"- {r.Title}: {r.BasePrice:N0} VNĐ/tháng, diện tích {r.SurfaceArea}m2, loại: {r.RoomType}, tại {r.Address}. (Số tiện ích: {r.AmenityCount})"));

                // 2. Xây dựng Prompt cho Gemini
                var systemPrompt = $"Bạn là trợ lý ảo thông minh của RoomHub - Nền tảng tìm kiếm phòng trọ hàng đầu. " +
                    $"Hãy trả lời người dùng một cách chuyên nghiệp, thân thiện và hữu ích bằng tiếng Việt. " +
                    $"Dưới đây là danh sách các phòng đang có sẵn trên hệ thống:\n{roomContext}\n\n" +
                    $"Hãy dựa vào danh sách trên để tư vấn cho người dùng. Nếu người dùng hỏi về phòng không có trong danh sách, hãy trả lời lịch sự rằng hiện chưa có thông tin đó.";

                var modelName = "gemini-2.5-flash";
                var requestUrl = $"https://generativelanguage.googleapis.com/v1/models/{modelName}:generateContent?key={_apiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = systemPrompt } } },
                        new { role = "model", parts = new[] { new { text = "Chào bạn! Tôi là trợ lý ảo của RoomHub. Tôi đã nắm rõ danh sách phòng hiện có. Tôi có thể giúp gì cho bạn?" } } },
                        new { role = "user", parts = new[] { new { text = userMessage } } }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    return $"DEBUG - AI API Error ({(int)response.StatusCode}): {errorMsg}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);
                
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? "Tôi không nhận được phản hồi rõ ràng từ AI.";
                    }
                }

                return "AI hiện đang bận, vui lòng thử lại sau giây lát.";
            }
            catch (Exception ex)
            {
                return $"DEBUG - System Error: {ex.Message}";
            }
        }
    }
}
