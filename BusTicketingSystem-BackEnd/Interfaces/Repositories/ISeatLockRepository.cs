using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISeatLockRepository : IRepository<SeatLock>
    {
        Task<SeatLock?> GetActiveLockAsync(int seatId);
        Task<List<SeatLock>> GetUserLocksAsync(int scheduleId, int userId);
        Task<bool> HasActiveLockAsync(int seatId, int userId);
        Task<int> CleanupExpiredLocksAsync();
        Task<List<SeatLock>> GetExpiredLocksAsync();
        Task<int> ExtendUserLocksAsync(int scheduleId, int userId, int extendByMinutes);
    }
}