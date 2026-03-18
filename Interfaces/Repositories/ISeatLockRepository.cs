using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISeatLockRepository
    {
        Task AddAsync(SeatLock seatLock);

        Task<SeatLock?> GetActiveLockAsync(int seatId);

        Task<List<SeatLock>> GetUserLocksAsync(int scheduleId, int userId);

        Task UpdateAsync(SeatLock seatLock);

        Task<SeatLock?> GetByIdAsync(int seatLockId);

        Task<bool> HasActiveLockAsync(int seatId, int userId);

        Task<int> CleanupExpiredLocksAsync();

        Task<List<SeatLock>> GetExpiredLocksAsync();

        Task SaveChangesAsync();
    }
}
