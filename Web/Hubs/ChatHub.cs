using Microsoft.AspNetCore.SignalR;

namespace Web.Hubs
{
    // Hub là nơi nhận và phân phối tin nhắn đến các client (trình duyệt) đang kết nối
    public class ChatHub : Hub
    {
        // Hàm này sẽ được gọi từ mã JavaScript ở trình duyệt (Frontend)
        public async Task SendMessage(string senderRole, string message)
        {
            // Tạm thời ở bước này: Phát lại tin nhắn cho TẤT CẢ các tab đang mở
            // (Sau này chúng ta sẽ tối ưu chỉ gửi cho đúng RoomId/UserId)
            await Clients.All.SendAsync("ReceiveMessage", senderRole, message);
        }
    }
}