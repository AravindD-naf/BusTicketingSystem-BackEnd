using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISeatRepository : IRepository<Seat>
    {
        Task<List<Seat>> GetSeatsByScheduleIdAsync(int scheduleId);
        Task<List<Seat>> GetSeatsByNumbersAsync(int scheduleId, List<string> seatNumbers);
        Task<Seat?> GetSeatByScheduleAndNumberAsync(int scheduleId, string seatNumber);
        Task UpdateManyAsync(List<Seat> seats);
        Task<bool> IsAvailableAsync(int seatId);
        Task<List<Seat>> GetLockedSeatsByUserAsync(int scheduleId, int userId);
        Task<int> CleanupExpiredLocksAsync();
    }
}