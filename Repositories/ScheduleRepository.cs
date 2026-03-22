using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Repositories
{
    public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
    {
        public ScheduleRepository(ApplicationDbContext context) : base(context) { }

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

        public async Task<(IEnumerable<Schedule>, int)>GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s => !s.IsDeleted);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(s =>
                    s.Route.Source.ToLower().Contains(kw) ||
                    s.Route.Destination.ToLower().Contains(kw) ||
                    s.Bus.BusNumber.ToLower().Contains(kw) ||
                    s.Bus.OperatorName.ToLower().Contains(kw));
            }

            var totalCount = await query.CountAsync();

            var schedules = await query
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
            var from = fromCity.Trim().ToLower();
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Source.ToLower().Trim() == from &&
                    s.IsActive &&
                    !s.IsDeleted)
                        .OrderBy(s => s.TravelDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();
        }

        public async Task<List<Schedule>> GetByToCityAsync(string toCity)
        {
            var to = toCity.Trim().ToLower();
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Destination.ToLower().Trim() == to &&
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
            var from = fromCity.Trim().ToLower();
            var to = toCity.Trim().ToLower();
            var date = travelDate.Date;

            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Source.ToLower().Trim() == from &&
                    s.Route.Destination.ToLower().Trim() == to &&
                    s.TravelDate.Date == date &&
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.AvailableSeats > 0)
                .OrderBy(s => s.DepartureTime)
                .ToListAsync();
        }


        public async Task<bool> HasOverlappingScheduleAsync(
            int busId,
            DateTime travelDate,
            TimeSpan departureTime,
            TimeSpan arrivalTime,
            bool isOvernight,
            int? excludeScheduleId = null)
        {
            // Get all schedules for this bus on this date (and previous day for overnight)
            var schedules = await _context.Schedules
                .Where(s =>
                    s.BusId == busId &&
                    !s.IsDeleted &&
                    (s.TravelDate.Date == travelDate.Date ||
                     s.TravelDate.Date == travelDate.Date.AddDays(-1)) &&
                    (excludeScheduleId == null || s.ScheduleId != excludeScheduleId))
                .ToListAsync();

            // Convert new schedule to minute ranges for comparison
            int newDepMins = (int)departureTime.TotalMinutes;
            int newArrMins = isOvernight
                ? newDepMins + (int)(arrivalTime.TotalMinutes == 0
                    ? 1440 : arrivalTime.TotalMinutes) + (arrivalTime.TotalMinutes < departureTime.TotalMinutes ? 1440 : 0)
                : (int)arrivalTime.TotalMinutes;

            // Recalculate properly
            // depMins is 0-1439, arrMins for overnight is depMins + journey duration
            // Let's compute journey duration from the incoming times
            int journeyDuration = isOvernight
                ? (1440 - newDepMins) + (int)arrivalTime.TotalMinutes
                : (int)arrivalTime.TotalMinutes - newDepMins;

            if (journeyDuration <= 0) journeyDuration += 1440;
            int newEnd = newDepMins + journeyDuration;

            foreach (var s in schedules)
            {
                int sDepMins = (int)s.DepartureTime.TotalMinutes;
                int sJourney = s.IsOvernightArrival
                    ? (1440 - sDepMins) + (int)s.ArrivalTime.TotalMinutes
                    : (int)s.ArrivalTime.TotalMinutes - sDepMins;
                if (sJourney <= 0) sJourney += 1440;
                int sEnd = sDepMins + sJourney;

                // Add 1440 offset for schedules from previous day
                if (s.TravelDate.Date == travelDate.Date.AddDays(-1))
                    sDepMins += 1440;

                // Check overlap: two ranges [a,b] and [c,d] overlap if a < d && c < b
                bool overlaps = newDepMins < sDepMins + sJourney &&
                                sDepMins < newDepMins + journeyDuration;

                if (overlaps) return true;
            }

            return false;
        }

        public async Task<(IEnumerable<Schedule>, int)> SearchAsync(
            string? keyword,
            int pageNumber,
            int pageSize)
        {
            var kw = keyword?.Trim().ToLower() ?? "";

            var query = _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s => !s.IsDeleted && (
                    string.IsNullOrEmpty(kw) ||
                    s.Route.Source.ToLower().Contains(kw) ||
                    s.Route.Destination.ToLower().Contains(kw) ||
                    s.Bus.BusNumber.ToLower().Contains(kw) ||
                    s.Bus.OperatorName.ToLower().Contains(kw)))
                .OrderByDescending(s => s.TravelDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}