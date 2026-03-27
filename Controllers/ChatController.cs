using BusTicketingSystem.Data;
using BusTicketingSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusTicketingSystem.Controllers
{
    [Route("api/v1/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatController(ApplicationDbContext db) => _db = db;

        /// GET /api/v1/chat/history/{otherUserId} — last 100 messages between caller and otherUser
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetHistory(int otherUserId)
        {
            var myId = GetUserId();
            var messages = await _db.ChatMessages
                .Where(m => !m.IsDeleted &&
                    ((m.SenderId == myId && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == myId)))
                .OrderBy(m => m.SentAt)
                .TakeLast(100)
                .Select(m => new {
                    m.MessageId, m.SenderId, m.ReceiverId,
                    m.Content, m.SentAt, m.IsReadByReceiver
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(messages));
        }

        /// GET /api/v1/chat/unread-counts — for admin: unread count per user
        [HttpGet("unread-counts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUnreadCounts()
        {
            var myId = GetUserId();
            var counts = await _db.ChatMessages
                .Where(m => m.ReceiverId == myId && !m.IsReadByReceiver && !m.IsDeleted)
                .GroupBy(m => m.SenderId)
                .Select(g => new { userId = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(counts));
        }

        /// GET /api/v1/chat/conversations — admin: list of users who have messaged
        [HttpGet("conversations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetConversations()
        {
            var myId = GetUserId();
            var userIds = await _db.ChatMessages
                .Where(m => !m.IsDeleted && (m.SenderId == myId || m.ReceiverId == myId))
                .Select(m => m.SenderId == myId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var users = await _db.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.FullName, u.Email })
                .ToListAsync();

            // Attach unread count per user
            var unread = await _db.ChatMessages
                .Where(m => m.ReceiverId == myId && !m.IsReadByReceiver && !m.IsDeleted)
                .GroupBy(m => m.SenderId)
                .Select(g => new { userId = g.Key, count = g.Count() })
                .ToDictionaryAsync(x => x.userId, x => x.count);

            var result = users.Select(u => new {
                u.UserId, u.FullName, u.Email,
                unreadCount = unread.TryGetValue(u.UserId, out var c) ? c : 0
            });

            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        /// GET /api/v1/chat/admin-id — returns the admin user id so customers know who to message
        [HttpGet("admin-id")]
        public async Task<IActionResult> GetAdminId()
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
            if (admin == null) return NotFound();
            return Ok(ApiResponse<object>.SuccessResponse(new { adminId = admin.UserId }));
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("nameid")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
