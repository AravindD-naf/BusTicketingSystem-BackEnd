using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BusTicketingSystem.Repositories
{
    public class AuditRepository : Repository<AuditLog>, IAuditRepository
    {
        public AuditRepository(ApplicationDbContext context) : base(context) { }

        public async Task AddAsync(AuditLog log)
        {
            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLog>, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? entityName,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(a => a.EntityName == entityName);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<Dictionary<int, string>> GetUserEmailsAsync(List<int> userIds)
        {
            if (!userIds.Any()) return new Dictionary<int, string>();

            return await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Email);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task LogAuditAsync(
            string action,
            string entityName,
            string entityId,
            object? oldValues,
            object? newValues,
            int userId,
            string ipAddress)
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues != null
                    ? JsonSerializer.Serialize(oldValues)
                    : null,
                NewValues = newValues != null
                    ? JsonSerializer.Serialize(newValues)
                    : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };


            //        private async Task LogAudit(string action, Models.Route route, object? oldValues, object? newValues, int userId, string ipAddress)
            //{
            //    var audit = new AuditLog
            //    {
            //        UserId = userId,
            //        Action = action,
            //        EntityName = "Route",
            //        EntityId = route.RouteId.ToString(),
            //        OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            //        NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            //        IpAddress = ipAddress,
            //        Timestamp = DateTime.UtcNow
            //    };

            await AddAsync(audit);
}
    }
}