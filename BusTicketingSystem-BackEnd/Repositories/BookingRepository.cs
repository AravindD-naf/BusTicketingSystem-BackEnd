using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context) { }

        public new async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await Task.CompletedTask;
        }

        public async Task<List<Booking>> GetExpiredPendingBookingsAsync()
        {
            // A booking is expired if it has been Pending for more than 5 minutes
            var expiryCutoff = DateTime.UtcNow.AddMinutes(-5);

            return await _context.Bookings
                .Where(b =>
                    b.BookingStatus == BookingStatus.Pending &&
                    b.BookingDate <= expiryCutoff &&
                    !b.IsDeleted)
                .ToListAsync();
        }

        public async Task<(List<Booking> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Bookings
                .Include(b => b.Refund)          // <-- add this Include
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BookingId);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }


        public new async Task<Booking?> GetByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                .Include(b => b.User)
                .Include(b => b.Seats)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    !b.IsDeleted);
        }

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.Schedule)
                .Include(b => b.User)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Schedule)
                .Where(b =>
                    b.UserId == userId &&
                    !b.IsDeleted)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public new async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Booking>> GetByUserIdWithRefundAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Route)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Bus)
                .Include(b => b.Refund)
                .Include(b => b.Seats)
                .Include(b => b.BusRating)
                .Include(b => b.Passengers)
                .Where(b => b.UserId == userId && !b.IsDeleted)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .IgnoreQueryFilters()
                .CountAsync(b => b.UserId == userId);
        }

    }
}