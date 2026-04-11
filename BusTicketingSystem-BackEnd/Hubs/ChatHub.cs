using BusTicketingSystem.Data;
using BusTicketingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BusTicketingSystem.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;

        // Thread-safe map: userId → connectionId
        private static readonly ConcurrentDictionary<int, string> _connections = new();

        public ChatHub(ApplicationDbContext db) => _db = db;

        public override async Task OnConnectedAsync() // when the user opens their chat their userid is stored in dictionary
        {
            var userId = GetUserId();
            if (userId > 0) _connections[userId] = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)  // when the user closes the tab or browser their entry is removed.
        {
            var userId = GetUserId();
            if (userId > 0) _connections.TryRemove(userId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Send a message to a specific user.</summary>
        public async Task SendMessage(int receiverId, string content)
        {
            var senderId = GetUserId();
            if (senderId <= 0 || string.IsNullOrWhiteSpace(content)) return;

            var message = new ChatMessage
            {
                SenderId   = senderId,
                ReceiverId = receiverId,
                Content    = content.Trim(),
                SentAt     = DateTime.UtcNow
            };
            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            var payload = new
            {
                messageId  = message.MessageId,
                senderId   = message.SenderId,
                receiverId = message.ReceiverId,
                content    = message.Content,
                sentAt     = message.SentAt
            };

            // Deliver to receiver if online
            if (_connections.TryGetValue(receiverId, out var receiverConn))
                await Clients.Client(receiverConn).SendAsync("ReceiveMessage", payload);

            // Echo back to sender
            await Clients.Caller.SendAsync("ReceiveMessage", payload);
        }

        /// <summary>Mark all messages from a sender as read.</summary>
        public async Task MarkRead(int senderId)
        {
            var myId = GetUserId();
            var unread = await _db.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == myId && !m.IsReadByReceiver)
                .ToListAsync();
            foreach (var m in unread) m.IsReadByReceiver = true;
            await _db.SaveChangesAsync();
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("nameid")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
