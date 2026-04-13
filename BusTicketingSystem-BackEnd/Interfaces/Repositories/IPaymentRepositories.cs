using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByBookingIdAsync(int bookingId);
        Task<List<Payment>> GetPendingPaymentsAsync();
        Task<List<Payment>> GetExpiredPaymentsAsync();
        Task UpdateAsync(Payment payment);
    }

    public interface IRefundRepository : IRepository<Refund>
    {
        Task<Refund?> GetByBookingIdAsync(int bookingId);
        Task<List<Refund>> GetPendingRefundsAsync();
        Task UpdateAsync(Refund refund);
    }

    public interface IPassengerRepository : IRepository<Passenger>
    {
        Task AddManyAsync(IEnumerable<Passenger> passengers);

        Task<List<Passenger>> GetByBookingIdAsync(int bookingId);
        Task<List<Passenger>> GetByBookingIdsAsync(List<int> bookingIds);
        Task UpdateAsync(Passenger passenger);
        Task DeleteAsync(int passengerId);
    }

    public interface ICancellationPolicyRepository : IRepository<CancellationPolicy>
    {
        Task<List<CancellationPolicy>> GetAllActiveAsync();
        Task<CancellationPolicy?> GetByHoursAsync(int hours);
        Task UpdateAsync(CancellationPolicy policy);
    }

    public interface IBusRatingRepository : IRepository<BusRating>
    {
        Task<BusRating?> GetByBookingIdAsync(int bookingId);
        Task<double> GetAverageRatingForBusAsync(int busId);
        Task UpdateBusRatingAverageAsync(int busId, double average);
    }
}