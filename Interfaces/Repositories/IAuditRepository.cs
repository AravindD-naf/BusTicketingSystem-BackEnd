using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<Dictionary<int, string>> GetUserEmailsAsync(List<int> userIds);


        Task<(List<AuditLog>, int totalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize,
            string? entityName,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate);

        Task SaveChangesAsync();

        Task LogAuditAsync(
            string action,
            string entityName,
            string entityId,
            object? oldValues,
            object? newValues,
            int userId,
            string ipAddress);
    }
}