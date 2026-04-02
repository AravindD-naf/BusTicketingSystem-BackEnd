using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class ErrorLogRepository : Repository<ErrorLog>, IErrorLogRepository
    {
        public ErrorLogRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<ErrorLog>> GetPagedAsync(int pageNumber, int pageSize, string? errorCode, bool? isCritical)
        {
            var query = GetQueryable();

            if (!string.IsNullOrEmpty(errorCode))
                query = query.Where(e => e.ErrorCode.Contains(errorCode));

            if (isCritical.HasValue)
                query = query.Where(e => e.IsCritical == isCritical.Value);

            return await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task DeleteOldResolvedAsync(DateTime cutoffDate)
        {
            var oldErrors = await GetQueryable()
                .Where(e => e.CreatedAt < cutoffDate && e.ResolvedAt != null)
                .ToListAsync();

            if (oldErrors.Count > 0)
                RemoveRange(oldErrors);
        }
    }
}