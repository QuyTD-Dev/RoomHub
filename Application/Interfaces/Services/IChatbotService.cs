using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IChatbotService
    {
        Task<string> GetChatResponseAsync(string userMessage);
    }
}
