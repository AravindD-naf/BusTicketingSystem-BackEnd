using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISeatRepository
    {
        Task<List<Seat>> GetSeatsByScheduleIdAsync(int scheduleId);

        Task<Seat?> GetByIdAsync(int seatId);

        Task<List<Seat>> GetSeatsByNumbersAsync(int scheduleId, List<string> seatNumbers);

        Task<Seat?> GetSeatByScheduleAndNumberAsync(int scheduleId, string seatNumber);

        Task AddAsync(Seat seat);
        Task UpdateAsync(Seat seat);
        Task UpdateManyAsync(List<Seat> seats);

        Task<bool> IsAvailableAsync(int seatId);

        Task<List<Seat>> GetLockedSeatsByUserAsync(int scheduleId, int userId);

        Task<int> CleanupExpiredLocksAsync();

        Task SaveChangesAsync();
    }
}
