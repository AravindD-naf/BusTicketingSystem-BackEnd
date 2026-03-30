using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IScheduleRepository : IRepository<Schedule>
    {
        Task<(IEnumerable<Schedule> schedules, int totalCount)>GetPagedAsync(int pageNumber, int pageSize, string? keyword = null);
        Task<bool> ExistsAsync(int busId, DateTime travelDate, TimeSpan departureTime);
        Task<bool> ExistsForUpdateAsync(int busId, int routeId, DateTime travelDate, TimeSpan departureTime, int currentScheduleId);
        Task<List<Schedule>> GetByFromCityAsync(string fromCity);
        Task<List<Schedule>> GetByToCityAsync(string toCity);
        Task<(List<Schedule> items, int totalCount)> SearchSchedulesAsync(ScheduleSearchRequest request);
        Task<List<Schedule>> SearchSchedulesAsync(string fromCity, string toCity, DateTime travelDate);
        // Add these two methods
        Task<bool> HasActiveBookingsForBusAsync(int busId);
        Task<bool> HasActiveBookingsForRouteAsync(int routeId);
        Task UpdateAsync(Schedule schedule);

        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<bool> HasOverlappingScheduleAsync(int busId, DateTime travelDate, TimeSpan departureTime, TimeSpan arrivalTime, bool isOvernight, int? excludeScheduleId = null);
        Task<(IEnumerable<Schedule>, int)> SearchAsync(string? keyword, int pageNumber, int pageSize);
        Task<bool> HasFutureSchedulesForBusAsync(int busId);
        Task<bool> HasFutureSchedulesForRouteAsync(int routeId);
        Task<int> MarkPastSchedulesInactiveAsync();


    }
}