using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task AddAsync(Booking booking);

        Task UpdateAsync(Booking booking);

        Task<Booking?> GetByIdAsync(int bookingId);

        Task<List<Booking>> GetAllAsync();

        Task<List<Booking>> GetByUserIdAsync(int userId);

        Task SaveChangesAsync();
    }
}