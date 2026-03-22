using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Web.Hubs
{
    [Authorize]
    public class VideoCallHub : Hub
    {
        // Track ai đang trong cuộc gọi: userId -> otherUserId
        private static readonly ConcurrentDictionary<string, string> _activeCallUsers = new();

        // ===================== CALLER: Gọi cho user khác =====================
        public async Task CallUser(string receiverId, string callerName)
        {
            string callerId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] CallUser: {callerId} -> {receiverId}");

            if (callerId == receiverId) return;

            // Check BUSY: nếu receiver đang trong cuộc gọi khác
            if (_activeCallUsers.ContainsKey(receiverId))
            {
                Console.WriteLine($"[VideoCall] {receiverId} is BUSY");
                await Clients.User(callerId).SendAsync("CallBusy");
                return;
            }

            // Check BUSY: nếu caller đang trong cuộc gọi khác
            if (_activeCallUsers.ContainsKey(callerId))
            {
                Console.WriteLine($"[VideoCall] {callerId} is already in a call");
                await Clients.User(callerId).SendAsync("CallBusy");
                return;
            }

            // Gửi thông báo cuộc gọi đến cho receiver
            await Clients.User(receiverId).SendAsync("IncomingCall", callerId, callerName);
        }

        // ===================== RECEIVER: Chấp nhận cuộc gọi =====================
        public async Task AcceptCall(string callerId)
        {
            string receiverId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] AcceptCall: {receiverId} accepted call from {callerId}");

            // Đánh dấu cả 2 đang trong cuộc gọi
            _activeCallUsers[callerId] = receiverId;
            _activeCallUsers[receiverId] = callerId;

            // Thông báo caller rằng cuộc gọi được chấp nhận → caller sẽ tạo offer
            await Clients.User(callerId).SendAsync("CallAccepted", receiverId);
        }

        // ===================== RECEIVER: Từ chối cuộc gọi =====================
        public async Task RejectCall(string callerId)
        {
            string receiverId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] RejectCall: {receiverId} rejected call from {callerId}");

            await Clients.User(callerId).SendAsync("CallRejected");
        }

        // ===================== Kết thúc cuộc gọi =====================
        public async Task HangUp(string otherUserId)
        {
            string userId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] HangUp: {userId} -> {otherUserId}");

            // Cleanup state
            _activeCallUsers.TryRemove(userId, out _);
            _activeCallUsers.TryRemove(otherUserId, out _);

            await Clients.User(otherUserId).SendAsync("CallEnded");
        }

        // ===================== WebRTC Signaling =====================

        /// <summary>Caller gửi SDP Offer → Receiver</summary>
        public async Task SendOffer(string receiverId, string offer)
        {
            string callerId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] SendOffer: {callerId} -> {receiverId} (offer length: {offer.Length})");
            await Clients.User(receiverId).SendAsync("ReceiveOffer", callerId, offer);
        }

        /// <summary>Receiver gửi SDP Answer → Caller</summary>
        public async Task SendAnswer(string callerId, string answer)
        {
            string receiverId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] SendAnswer: {receiverId} -> {callerId} (answer length: {answer.Length})");
            await Clients.User(callerId).SendAsync("ReceiveAnswer", receiverId, answer);
        }

        /// <summary>Gửi ICE Candidate cho peer</summary>
        public async Task SendIceCandidate(string otherUserId, string candidate)
        {
            string userId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] SendIceCandidate: {userId} -> {otherUserId}");
            await Clients.User(otherUserId).SendAsync("ReceiveIceCandidate", userId, candidate);
        }

        // ===================== Lifecycle: Handle disconnect =====================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = Context.UserIdentifier!;
            Console.WriteLine($"[VideoCall] OnDisconnected: {userId}, Reason: {exception?.Message ?? "normal"}");

            // Nếu user đang trong cuộc gọi, tự động HangUp
            if (_activeCallUsers.TryRemove(userId, out string? otherUserId))
            {
                _activeCallUsers.TryRemove(otherUserId!, out _);
                await Clients.User(otherUserId!).SendAsync("CallEnded");
                Console.WriteLine($"[VideoCall] Auto-HangUp: {userId} disconnected, notified {otherUserId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
