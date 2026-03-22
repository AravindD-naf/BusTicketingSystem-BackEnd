using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IAuditRepository : IRepository<AuditLog>
    {
        Task<Dictionary<int, string>> GetUserEmailsAsync(List<int> userIds);


        Task<(List<AuditLog>, int totalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize,
            string? entityName,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate);

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