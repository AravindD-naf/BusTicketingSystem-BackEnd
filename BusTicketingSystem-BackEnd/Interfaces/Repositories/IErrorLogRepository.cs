using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IErrorLogRepository : IRepository<ErrorLog>
    {
        Task<List<ErrorLog>> GetPagedAsync(int pageNumber, int pageSize, string? errorCode, bool? isCritical);
        Task DeleteOldResolvedAsync(DateTime cutoffDate);
    }
}