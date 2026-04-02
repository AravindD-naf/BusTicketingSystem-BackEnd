using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BusTicketingSystem.Services
{   
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuditService(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
        }

        public async Task LogAsync(
            int? userId,
            string action,
            string entityName,
            string? entityId,
            object? oldValues,
            object? newValues,
            string? ipAddress)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues != null
                    ? JsonSerializer.Serialize(oldValues, _jsonOptions)
                    : null,
                NewValues = newValues != null
                    ? JsonSerializer.Serialize(newValues, _jsonOptions)
                    : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(log);
        }

        public async Task<(List<AuditLogResponse>, int totalCount)> GetPagedLogsAsync(
            int pageNumber,
            int pageSize,
            string? entityName,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var (logs, totalCount) = await _auditRepository
                .GetPagedAsync(pageNumber, pageSize, entityName, userId, fromDate, toDate);

            // Get all unique userIds to fetch emails in one query
            var userIds = logs.Where(a => a.UserId.HasValue)
                              .Select(a => a.UserId!.Value)
                              .Distinct()
                              .ToList();

            var userEmails = await _auditRepository.GetUserEmailsAsync(userIds);

            var response = logs.Select(a => new AuditLogResponse
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                UserEmail = a.UserId.HasValue && userEmails.ContainsKey(a.UserId.Value)
                    ? userEmails[a.UserId.Value]
                    : "System",
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                // Normalize ::1 (IPv6 loopback) to 127.0.0.1
                IpAddress = a.IpAddress == "::1" ? "127.0.0.1" : a.IpAddress,
                Timestamp = a.Timestamp
            }).ToList();

            return (response, totalCount);
        }
    }
}