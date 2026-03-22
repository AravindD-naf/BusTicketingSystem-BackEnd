using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class SeatRepository : Repository<Seat>, ISeatRepository
    {
        public SeatRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<Seat>> GetSeatsByScheduleIdAsync(int scheduleId)
        {
            return await _context.Seats
                .Where(s => s.ScheduleId == scheduleId && !s.IsDeleted)
                .OrderBy(s => s.SeatNumber.Substring(0, 1))   // row letter: A, B, C...
                .ThenBy(s => s.SeatNumber.Length)              // length first so A1 < A10
                .ThenBy(s => s.SeatNumber)                     // then lexicographic within same length
                .ToListAsync();
        }

        public async Task<Seat?> GetByIdAsync(int seatId)
        {
            return await _context.Seats
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(s => s.SeatId == seatId && !s.IsDeleted);
        }

        public async Task<List<Seat>> GetSeatsByNumbersAsync(int scheduleId, List<string> seatNumbers)
        {
            return await _context.Seats
                .Where(s => s.ScheduleId == scheduleId && 
                       seatNumbers.Contains(s.SeatNumber) && 
                       !s.IsDeleted)
                .ToListAsync();
        }

        public async Task<Seat?> GetSeatByScheduleAndNumberAsync(int scheduleId, string seatNumber)
        {
            return await _context.Seats
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && 
                                    s.SeatNumber == seatNumber && 
                                    !s.IsDeleted);
        }

        public async Task AddAsync(Seat seat)
        {
            await _context.Seats.AddAsync(seat);
        }

        public async Task UpdateAsync(Seat seat)
        {
            _context.Seats.Update(seat);
            await Task.CompletedTask;
        }

        public async Task UpdateManyAsync(List<Seat> seats)
        {
            _context.Seats.UpdateRange(seats);
            await Task.CompletedTask;
        }

        public async Task<bool> IsAvailableAsync(int seatId)
        {
            var seat = await _context.Seats.FirstOrDefaultAsync(s => s.SeatId == seatId && !s.IsDeleted);
            return seat?.SeatStatus == "Available";
        }

        public async Task<List<Seat>> GetLockedSeatsByUserAsync(int scheduleId, int userId)
        {
            return await _context.Seats
                .Where(s => s.ScheduleId == scheduleId && 
                       s.LockedByUserId == userId && 
                       s.SeatStatus == "Locked" && 
                       !s.IsDeleted)
                .ToListAsync();
        }

        public async Task<int> CleanupExpiredLocksAsync()
        {
            var now = DateTime.UtcNow;

            var expiredSeats = await _context.Seats
                .Where(s => s.SeatStatus == "Locked" && 
                       s.LockedAt.HasValue && 
                       s.LockedAt.Value.AddMinutes(5) <= now && 
                       !s.IsDeleted)
                .ToListAsync();

            foreach (var seat in expiredSeats)
            {
                seat.SeatStatus = "Available";
                seat.LockedByUserId = null;
                seat.LockedAt = null;
                seat.BookingId = null;
                seat.UpdatedAt = now;
            }

            _context.Seats.UpdateRange(expiredSeats);
            await _context.SaveChangesAsync();

            return expiredSeats.Count;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
