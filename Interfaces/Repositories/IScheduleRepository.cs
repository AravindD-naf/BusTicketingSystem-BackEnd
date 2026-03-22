using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<Schedule?> GetByIdAsync(int id);
        Task<(IEnumerable<Schedule> schedules, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<bool> ExistsAsync(int busId, DateTime travelDate, TimeSpan departureTime);
        Task UpdateAsync(Schedule schedule);
        Task<bool> ExistsForUpdateAsync(int busId, int routeId, DateTime travelDate, TimeSpan departureTime, int currentScheduleId);
        Task<List<Schedule>> GetByFromCityAsync(string fromCity);
        Task<List<Schedule>> GetByToCityAsync(string toCity);
        Task<List<Schedule>> SearchSchedulesAsync(string fromCity, string toCity, DateTime travelDate);
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<bool> HasOverlappingScheduleAsync(int busId, DateTime travelDate, TimeSpan departureTime, TimeSpan arrivalTime, bool isOvernight, int? excludeScheduleId = null);
        Task<(IEnumerable<Schedule>, int)> SearchAsync(string? keyword, int pageNumber, int pageSize);
    }
}