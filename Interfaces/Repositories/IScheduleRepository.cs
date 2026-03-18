using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IScheduleRepository
    {
        Task AddAsync(Schedule schedule);

        Task<Schedule?> GetByIdAsync(int id);

        Task<(IEnumerable<Schedule> schedules, int totalCount)>
            GetPagedAsync(int pageNumber, int pageSize);

        Task<bool> ExistsAsync(int busId, DateTime travelDate, TimeSpan departureTime);

        Task UpdateAsync(Schedule schedule);

        Task<bool> ExistsForUpdateAsync(
            int busId,
            int routeId,
            DateTime travelDate,
            TimeSpan departureTime,
            int currentScheduleId);

        Task SaveChangesAsync();

        Task<List<Schedule>> GetByFromCityAsync(string fromCity);

        Task<List<Schedule>> GetByToCityAsync(string toCity);

        Task<List<Schedule>> SearchSchedulesAsync(
            string fromCity,
            string toCity,
            DateTime travelDate);

        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}