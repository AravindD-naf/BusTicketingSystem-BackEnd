using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public ScheduleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Schedule schedule)
        {
            await _context.Schedules.AddAsync(schedule);
        }

        public async Task<Schedule?> GetByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.ScheduleId == id && !s.IsDeleted);
        }

        public async Task<(IEnumerable<Schedule>, int)>
            GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Schedules
                .Where(s => !s.IsDeleted);

            var totalCount = await query.CountAsync();

            var schedules = await query
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .OrderByDescending(s => s.TravelDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (schedules, totalCount);
        }

        public async Task<bool> ExistsAsync(
            int busId,
            DateTime travelDate,
            TimeSpan departureTime)
        {
            return await _context.Schedules.AnyAsync(s =>
                s.BusId == busId &&
                s.TravelDate.Date == travelDate.Date &&
                s.DepartureTime == departureTime &&
                !s.IsDeleted);
        }

        public Task UpdateAsync(Schedule schedule)
        {
            _context.Schedules.Update(schedule);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsForUpdateAsync(
            int busId,
            int routeId,
            DateTime travelDate,
            TimeSpan departureTime,
            int currentScheduleId)
        {
            return await _context.Schedules
                .AnyAsync(s =>
                    s.BusId == busId &&
                    s.RouteId == routeId &&
                    s.TravelDate == travelDate &&
                    s.DepartureTime == departureTime &&
                    s.ScheduleId != currentScheduleId &&
                    !s.IsDeleted);
        }

        public async Task<List<Schedule>> GetByFromCityAsync(string fromCity)
        {
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Source == fromCity &&
                    s.IsActive &&
                    !s.IsDeleted)
                .OrderBy(s => s.TravelDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();
        }

        public async Task<List<Schedule>> GetByToCityAsync(string toCity)
        {
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Destination == toCity &&
                    s.IsActive &&
                    !s.IsDeleted)
                .OrderBy(s => s.TravelDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();
        }

        public async Task<List<Schedule>> SearchSchedulesAsync(
            string fromCity,
            string toCity,
            DateTime travelDate)
        {
            travelDate = travelDate.Date;

            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Source == fromCity &&
                    s.Route.Destination == toCity &&
                    s.TravelDate == travelDate &&
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.AvailableSeats > 0)
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}