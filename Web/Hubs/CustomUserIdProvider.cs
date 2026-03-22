using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Web.Hubs
{
    /// <summary>
    /// Custom IUserIdProvider đảm bảo SignalR map đúng UserId từ Identity Claims.
    /// Ưu tiên ClaimTypes.NameIdentifier (ASP.NET Identity default).
    /// </summary>
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
