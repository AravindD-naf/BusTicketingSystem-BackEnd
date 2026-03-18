using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            int? userId,
            string action,
            string entityName,
            string? entityId,
            object? oldValues,
            object? newValues,
            string? ipAddress);

        Task<(List<AuditLogResponse>, int totalCount)> GetPagedLogsAsync(
            int pageNumber,
            int pageSize,
            string? entityName,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate
            );
    }
}