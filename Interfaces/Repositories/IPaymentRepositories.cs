using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int paymentId);
        Task<Payment?> GetByBookingIdAsync(int bookingId);
        Task<List<Payment>> GetPendingPaymentsAsync();
        Task<List<Payment>> GetExpiredPaymentsAsync();
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }

    public interface IRefundRepository
    {
        Task AddAsync(Refund refund);
        Task<Refund?> GetByIdAsync(int refundId);
        Task<Refund?> GetByBookingIdAsync(int bookingId);
        Task<List<Refund>> GetPendingRefundsAsync();
        Task UpdateAsync(Refund refund);
        Task SaveChangesAsync();
    }

    public interface IPassengerRepository
    {
        Task AddAsync(Passenger passenger);
        Task AddManyAsync(IEnumerable<Passenger> passengers);
        Task<Passenger?> GetByIdAsync(int passengerId);
        Task<List<Passenger>> GetByBookingIdAsync(int bookingId);
        Task UpdateAsync(Passenger passenger);
        Task DeleteAsync(int passengerId);
        Task SaveChangesAsync();
    }

    public interface ICancellationPolicyRepository
    {
        Task<List<CancellationPolicy>> GetAllActiveAsync();
        Task<CancellationPolicy?> GetByHoursAsync(int hours);
        Task AddAsync(CancellationPolicy policy);
        Task UpdateAsync(CancellationPolicy policy);
        Task SaveChangesAsync();
    }
}
