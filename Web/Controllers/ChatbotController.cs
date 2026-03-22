using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest("Nội dung tin nhắn không được để trống.");
            }

            var response = await _chatbotService.GetChatResponseAsync(request.Message);
            return Ok(new { response });
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
