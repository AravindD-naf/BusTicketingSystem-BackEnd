using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context) { }

        public new async Task AddAsync(Payment payment) => await _context.Payments.AddAsync(payment);

        public new async Task<Payment?> GetByIdAsync(int paymentId) =>
            await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && !p.IsDeleted);

        public async Task<Payment?> GetByBookingIdAsync(int bookingId) =>
            await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == bookingId && !p.IsDeleted);

        public async Task<List<Payment>> GetPendingPaymentsAsync() =>
            await _context.Payments
                .Where(p => p.Status == Models.Enums.PaymentStatus.Pending && !p.IsDeleted)
                .ToListAsync();

        public async Task<List<Payment>> GetExpiredPaymentsAsync() =>
            await _context.Payments
                .Where(p => p.ExpiresAt <= DateTime.UtcNow && 
                       p.Status == Models.Enums.PaymentStatus.Pending && 
                       !p.IsDeleted)
                .ToListAsync();

        public async Task UpdateAsync(Payment payment) => _context.Payments.Update(payment);

        public new async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class RefundRepository : Repository<Refund>, IRefundRepository
    {
        public RefundRepository(ApplicationDbContext context) : base(context) { }

        public new async Task AddAsync(Refund refund) => await _context.Refunds.AddAsync(refund);

        public new async Task<Refund?> GetByIdAsync(int refundId) =>
            await _context.Refunds
                .FirstOrDefaultAsync(r => r.RefundId == refundId && !r.IsDeleted);

        public async Task<Refund?> GetByBookingIdAsync(int bookingId) =>
            await _context.Refunds
                .FirstOrDefaultAsync(r => r.BookingId == bookingId && !r.IsDeleted);

        public async Task<List<Refund>> GetPendingRefundsAsync() =>
            await _context.Refunds
                .Where(r => r.Status == Models.Enums.RefundStatus.Pending && !r.IsDeleted)
                .ToListAsync();

        public async Task UpdateAsync(Refund refund) => _context.Refunds.Update(refund);

        public new async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class PassengerRepository : Repository<Passenger>, IPassengerRepository
    {
        public PassengerRepository(ApplicationDbContext context) : base(context) { }

        public new async Task AddAsync(Passenger passenger) => await _context.Passengers.AddAsync(passenger);

        public async Task AddManyAsync(IEnumerable<Passenger> passengers)
        {
            await _context.Passengers.AddRangeAsync(passengers);
        }

        public new async Task<Passenger?> GetByIdAsync(int passengerId) =>
            await _context.Passengers
                .FirstOrDefaultAsync(p => p.PassengerId == passengerId && !p.IsDeleted);

        public async Task<List<Passenger>> GetByBookingIdAsync(int bookingId) =>
            await _context.Passengers
                .Where(p => p.BookingId == bookingId && !p.IsDeleted)
                .OrderBy(p => p.SeatNumber)
                .ToListAsync();

        public async Task UpdateAsync(Passenger passenger) => _context.Passengers.Update(passenger);

        public async Task DeleteAsync(int passengerId)
        {
            var passenger = await GetByIdAsync(passengerId);
            if (passenger != null)
            {
                passenger.IsDeleted = true;
                _context.Passengers.Update(passenger);
            }
        }

        public new async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }

    public class CancellationPolicyRepository : Repository<CancellationPolicy>, ICancellationPolicyRepository
    {
        public CancellationPolicyRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<CancellationPolicy>> GetAllActiveAsync() =>
            await _context.CancellationPolicies
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.HoursBeforeDeparture)
                .ToListAsync();

        public async Task<CancellationPolicy?> GetByHoursAsync(int hours) =>
            await _context.CancellationPolicies
                .FirstOrDefaultAsync(p => p.HoursBeforeDeparture == hours && p.IsActive);

        public new async Task AddAsync(CancellationPolicy policy) =>
            await _context.CancellationPolicies.AddAsync(policy);

        public async Task UpdateAsync(CancellationPolicy policy) =>
            _context.CancellationPolicies.Update(policy);

        public new async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
