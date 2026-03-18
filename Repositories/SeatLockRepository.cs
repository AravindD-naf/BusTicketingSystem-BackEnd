using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class SeatLockRepository : ISeatLockRepository
    {
        private readonly ApplicationDbContext _context;
        private const int LOCK_EXPIRY_MINUTES = 5;

        public SeatLockRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SeatLock seatLock)
        {
            await _context.SeatLocks.AddAsync(seatLock);
        }

        public async Task<SeatLock?> GetActiveLockAsync(int seatId)
        {
            var now = DateTime.UtcNow;

            return await _context.SeatLocks
                .Include(sl => sl.Seat)
                .Include(sl => sl.User)
                .FirstOrDefaultAsync(sl => 
                    sl.SeatId == seatId && 
                    !sl.IsReleased && 
                    sl.ExpiresAt > now);
        }

        public async Task<List<SeatLock>> GetUserLocksAsync(int scheduleId, int userId)
        {
            var now = DateTime.UtcNow;

            return await _context.SeatLocks
                .Include(sl => sl.Seat)
                .Where(sl => 
                    sl.Seat.ScheduleId == scheduleId && 
                    sl.UserId == userId && 
                    !sl.IsReleased && 
                    sl.ExpiresAt > now)
                .ToListAsync();
        }

        public async Task UpdateAsync(SeatLock seatLock)
        {
            _context.SeatLocks.Update(seatLock);
            await Task.CompletedTask;
        }

        public async Task<SeatLock?> GetByIdAsync(int seatLockId)
        {
            return await _context.SeatLocks
                .Include(sl => sl.Seat)
                .Include(sl => sl.User)
                .FirstOrDefaultAsync(sl => sl.SeatLockId == seatLockId);
        }

        public async Task<bool> HasActiveLockAsync(int seatId, int userId)
        {
            var now = DateTime.UtcNow;

            return await _context.SeatLocks
                .AnyAsync(sl => 
                    sl.SeatId == seatId && 
                    sl.UserId == userId && 
                    !sl.IsReleased && 
                    sl.ExpiresAt > now);
        }

        public async Task<int> CleanupExpiredLocksAsync()
        {
            var now = DateTime.UtcNow;

            var expiredLocks = await _context.SeatLocks
                .Where(sl => !sl.IsReleased && sl.ExpiresAt <= now)
                .ToListAsync();

            foreach (var @lock in expiredLocks)
            {
                @lock.IsReleased = true;
                @lock.ReleasedAt = now;
            }

            _context.SeatLocks.UpdateRange(expiredLocks);
            await _context.SaveChangesAsync();

            return expiredLocks.Count;
        }

        public async Task<List<SeatLock>> GetExpiredLocksAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.SeatLocks
                .Include(sl => sl.Seat)
                .Where(sl => !sl.IsReleased && sl.ExpiresAt <= now)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
