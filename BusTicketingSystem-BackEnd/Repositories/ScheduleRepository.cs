using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BusTicketingSystem.Repositories
{
    public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
    {
        public ScheduleRepository(ApplicationDbContext context) : base(context) { }

        public new async Task AddAsync(Schedule schedule)
        {
            await _context.Schedules.AddAsync(schedule);
        }

        public new async Task<Schedule?> GetByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.ScheduleId == id && !s.IsDeleted);
        }

        public async Task<bool> HasFutureSchedulesForBusAsync(int busId)
        {
            return await _context.Schedules
                .AnyAsync(s => s.BusId == busId &&
                               !s.IsDeleted &&
                               s.TravelDate.Date >= DateTime.UtcNow.Date);
        }

        public async Task<bool> HasFutureSchedulesForRouteAsync(int routeId)
        {
            return await _context.Schedules
                .AnyAsync(s => s.RouteId == routeId &&
                               !s.IsDeleted &&
                               s.TravelDate.Date >= DateTime.UtcNow.Date);
        }


        public async Task<(IEnumerable<Schedule>, int)>GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s => !s.IsDeleted && !s.Route.IsDeleted);

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

        public new async Task SaveChangesAsync()
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

        // Simple overload used by tests — returns matching schedules for a city/date
        public async Task<List<Schedule>> SearchSchedulesAsync(string fromCity, string toCity, DateTime travelDate)
        {
            var from = fromCity.Trim().ToLower();
            var to   = toCity.Trim().ToLower();
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
                .ToListAsync();
        }

        // REPLACE the existing SearchSchedulesAsync method with:
        public async Task<(List<Schedule> items, int totalCount)> SearchSchedulesAsync(ScheduleSearchRequest request)        {
            var from = request.FromCity.Trim().ToLower();
            var to   = request.ToCity.Trim().ToLower();
            var date = request.TravelDateUtc.Date;

            var query = _context.Schedules
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .Where(s =>
                    s.Route.Source.ToLower().Trim() == from &&
                    s.Route.Destination.ToLower().Trim() == to &&
                    s.TravelDate.Date == date &&
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.AvailableSeats > 0 &&
                    (s.TravelDate.Date > DateTime.UtcNow.Date ||
                     (s.TravelDate.Date == DateTime.UtcNow.Date && s.DepartureTime > DateTime.UtcNow.TimeOfDay)));

            // ── Filters ──
            if (request.BusTypes != null && request.BusTypes.Count > 0)
                query = query.Where(s => request.BusTypes.Contains(s.Bus.BusType));

            if (request.MaxPrice.HasValue)
                query = query.Where(s =>
                    (s.Fare > 0 ? s.Fare : s.Route.BaseFare) <= request.MaxPrice.Value);

            if (request.Operators != null && request.Operators.Count > 0)
                query = query.Where(s => request.Operators.Contains(s.Bus.OperatorName));

            if (request.MinRating.HasValue && request.MinRating.Value > 0)
                query = query.Where(s => s.Bus.RatingAverage >= request.MinRating.Value);

            if (request.DepartureTimes != null && request.DepartureTimes.Count > 0)
            {
                // Convert time-of-day labels to minute ranges and filter in memory
                // (EF can't translate custom time-range logic easily, so pull then filter)
                var all = await query.ToListAsync();
                all = all.Where(s => {
                    var h = (int)s.DepartureTime.TotalHours % 24;
                    return request.DepartureTimes.Any(t => t switch {
                        "morning"   => h >= 6  && h < 12,
                        "afternoon" => h >= 12 && h < 18,
                        "evening"   => h >= 18 && h < 22,
                        "night"     => h >= 22 || h < 6,
                        _           => false
                    });
                }).ToList();

                // Apply sort + pagination on the in-memory list
                all = ApplySortInMemory(all, request.SortBy);
                var totalInMem = all.Count;
                var pagedInMem = all
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();
                return (pagedInMem, totalInMem);
            }

            // ── Sort (DB-level) ──
            IOrderedQueryable<Schedule> ordered = request.SortBy switch {
                "price_asc"  => query.OrderBy(s => s.Fare > 0 ? s.Fare : s.Route.BaseFare),
                "price_desc" => query.OrderByDescending(s => s.Fare > 0 ? s.Fare : s.Route.BaseFare),
                "arrival"    => query.OrderBy(s => s.ArrivalTime),
                "rating"     => query.OrderByDescending(s => s.Bus.RatingAverage),
                _            => query.OrderBy(s => s.DepartureTime)  // default: departure
            };
            // duration sort is in-memory (needs computed field)
            if (request.SortBy == "duration")
            {
                var allForDuration = await query.ToListAsync();
                allForDuration = ApplySortInMemory(allForDuration, "duration");
                var total2 = allForDuration.Count;
                var paged2 = allForDuration
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();
                return (paged2, total2);
            }

            var totalCount = await ordered.CountAsync();
            var items = await ordered
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Helper for in-memory sorts
        private List<Schedule> ApplySortInMemory(List<Schedule> list, string? sortBy) =>
            sortBy switch {
                "price_asc"  => list.OrderBy(s => s.Fare > 0 ? s.Fare : s.Route?.BaseFare ?? 0).ToList(),
                "price_desc" => list.OrderByDescending(s => s.Fare > 0 ? s.Fare : s.Route?.BaseFare ?? 0).ToList(),
                "arrival"    => list.OrderBy(s => s.ArrivalTime).ToList(),
                "rating"     => list.OrderByDescending(s => s.Bus?.RatingAverage ?? 0).ToList(),
                "duration"   => list.OrderBy(s => {
                    var dep = (int)s.DepartureTime.TotalMinutes;
                    var arr = (int)s.ArrivalTime.TotalMinutes;
                    return s.IsOvernightArrival ? (1440 - dep) + arr : arr - dep;
                }).ToList(),
                _            => list.OrderBy(s => s.DepartureTime).ToList()
            };



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

        public async Task<bool> HasActiveBookingsForBusAsync(int busId)
        {
            return await _context.Schedules
                .Where(s => s.BusId == busId && !s.IsDeleted && s.TravelDate.Date >= DateTime.UtcNow.Date)
                .AnyAsync(s => _context.Bookings
                    .Any(b => b.ScheduleId == s.ScheduleId &&
                              !b.IsDeleted &&
                              (b.BookingStatus == BookingStatus.Pending ||
                               b.BookingStatus == BookingStatus.Confirmed)));
        }

        public async Task<bool> HasActiveBookingsForRouteAsync(int routeId)
        {
            return await _context.Schedules
                .Where(s => s.RouteId == routeId && !s.IsDeleted && s.TravelDate.Date >= DateTime.UtcNow.Date)
                .AnyAsync(s => _context.Bookings
                    .Any(b => b.ScheduleId == s.ScheduleId &&
                              !b.IsDeleted &&
                              (b.BookingStatus == BookingStatus.Pending ||
                               b.BookingStatus == BookingStatus.Confirmed)));
        }

        public async Task<int> MarkPastSchedulesInactiveAsync()
        {
            var now = DateTime.UtcNow;
            var pastSchedules = await _context.Schedules
                .Where(s =>
                    s.IsActive &&
                    !s.IsDeleted &&
                    (s.TravelDate.Date < now.Date ||
                     (s.TravelDate.Date == now.Date && s.DepartureTime < now.TimeOfDay)))
                .ToListAsync();

            if (pastSchedules.Count == 0) return 0;

            foreach (var s in pastSchedules)
            {
                s.IsActive = false;
                s.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return pastSchedules.Count;
        }



        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}