using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment?> GetByIdAsync(int paymentId);
        Task<Payment?> GetByBookingIdAsync(int bookingId);
        Task<List<Payment>> GetPendingPaymentsAsync();
        Task<List<Payment>> GetExpiredPaymentsAsync();
        Task UpdateAsync(Payment payment);
    }

    public interface IRefundRepository : IRepository<Refund>
    {
        Task<Refund?> GetByIdAsync(int refundId);
        Task<Refund?> GetByBookingIdAsync(int bookingId);
        Task<List<Refund>> GetPendingRefundsAsync();
        Task UpdateAsync(Refund refund);
    }

    public interface IPassengerRepository : IRepository<Passenger>
    {
        Task AddManyAsync(IEnumerable<Passenger> passengers);
        Task<Passenger?> GetByIdAsync(int passengerId);
        Task<List<Passenger>> GetByBookingIdAsync(int bookingId);
        Task UpdateAsync(Passenger passenger);
        Task DeleteAsync(int passengerId);
    }

    public interface ICancellationPolicyRepository : IRepository<CancellationPolicy>
    {
        Task<List<CancellationPolicy>> GetAllActiveAsync();
        Task<CancellationPolicy?> GetByHoursAsync(int hours);
        Task UpdateAsync(CancellationPolicy policy);
    }
}