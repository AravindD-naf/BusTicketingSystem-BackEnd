using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetByIdAsync(int bookingId);
        Task<(List<Booking> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<List<Booking>> GetAllAsync();
        Task<List<Booking>> GetByUserIdAsync(int userId);
        Task<List<Booking>> GetExpiredPendingBookingsAsync();
        Task UpdateAsync(Booking booking);
        Task<List<Booking>> GetByUserIdWithRefundAsync(int userId);

    }
}