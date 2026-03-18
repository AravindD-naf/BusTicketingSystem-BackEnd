using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using UnauthorizedAccessException = BusTicketingSystem.Exceptions.UnauthorizedAccessException;

namespace BusTicketingSystem.Services
{

    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IRefundRepository _refundRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ICancellationPolicyRepository _policyRepository;
        private readonly IAuditRepository _auditRepository;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IRefundRepository refundRepository,
            IBookingRepository bookingRepository,
            IScheduleRepository scheduleRepository,
            ICancellationPolicyRepository policyRepository,
            IAuditRepository auditRepository)
        {
            _paymentRepository = paymentRepository;
            _refundRepository = refundRepository;
            _bookingRepository = bookingRepository;
            _scheduleRepository = scheduleRepository;
            _policyRepository = policyRepository;
            _auditRepository = auditRepository;
        }

        public async Task<ApiResponse<PaymentResponseDto>> InitiatePaymentAsync(
            int bookingId,
            decimal amount,
            string paymentMethod,
            int userId,
            string ipAddress
            )
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("You cannot initiate payment for this booking.");

            if (booking.BookingStatus != BookingStatus.Pending)
                throw new PaymentOperationException(
                    "Can only initiate payment for Pending bookings",
                    PaymentOperationException.PaymentErrorType.ProcessingError);

            if (Math.Abs(amount - booking.TotalAmount) > 0.01m)
                throw new PaymentOperationException(
                    "Payment amount does not match booking total",
                    PaymentOperationException.PaymentErrorType.InvalidAmount);

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            await _paymentRepository.AddAsync(payment);

            booking.BookingStatus = BookingStatus.PaymentProcessing;
            booking.LastStatusChangeAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            await _paymentRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "INITIATE_PAYMENT",
                "Payment",
                payment.PaymentId.ToString(),
                null,
                new { bookingId, amount, paymentMethod },
                userId,
                ipAddress);

            return ApiResponse<PaymentResponseDto>.SuccessResponse(MapToDto(payment));
        }


        public async Task<ApiResponse<PaymentResponseDto>> GetPaymentAsync(int paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null || payment.IsDeleted)
                throw new ResourceNotFoundException("Payment", paymentId.ToString());

            return ApiResponse<PaymentResponseDto>.SuccessResponse(MapToDto(payment));
        }

        public async Task<ApiResponse<PaymentResponseDto>> ConfirmPaymentAsync(
            ConfirmPaymentRequestDto dto,
            int userId,
            string ipAddress)
        {
            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null || payment.IsDeleted)
                throw new ResourceNotFoundException("Payment", dto.PaymentId.ToString());

            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
            if (booking == null)
                throw new ResourceNotFoundException("Booking", payment.BookingId.ToString());

            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("Unauthorized payment confirmation.");

            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Processing)
                throw new PaymentOperationException(
                    $"Cannot confirm payment with status: {payment.Status}",
                    PaymentOperationException.PaymentErrorType.ProcessingError);

         
            payment.TransactionId = dto.TransactionId;
            payment.ProcessedAt = DateTime.UtcNow;

            if (dto.IsSuccess)
            {
                payment.Status = PaymentStatus.Success;
                booking.BookingStatus = BookingStatus.Confirmed;
                booking.LastStatusChangeAt = DateTime.UtcNow;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = dto.FailureReason;
                booking.BookingStatus = BookingStatus.PaymentFailed;
                booking.LastStatusChangeAt = DateTime.UtcNow;
            }

            await _paymentRepository.UpdateAsync(payment);
            await _bookingRepository.UpdateAsync(booking);
            await _paymentRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "CONFIRM_PAYMENT",
                "Payment",
                payment.PaymentId.ToString(),
                null,
                new { bookingId = booking.BookingId, success = dto.IsSuccess, transactionId = dto.TransactionId },
                userId,
                ipAddress);

            return ApiResponse<PaymentResponseDto>.SuccessResponse(MapToDto(payment));
        }


        public async Task<ApiResponse<RefundResponseDto>> InitiateRefundAsync(
            int bookingId,
            int userId,
            string ipAddress)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("You cannot initiate refund for this booking.");

            var payment = await _paymentRepository.GetByBookingIdAsync(bookingId);
            if (payment == null || payment.Status != PaymentStatus.Success)
                throw new RefundOperationException(
                    "Can only refund for confirmed payments",
                    RefundOperationException.RefundErrorType.InvalidRefund);

            // Calculate refund amount
            var (refundAmount, refundPercentage, cancellationFee) = await CalculateRefundAsync(bookingId);

            var refund = new Refund
            {
                BookingId = bookingId,
                PaymentId = payment.PaymentId,
                RefundAmount = refundAmount,
                CancellationFee = cancellationFee,
                RefundPercentage = refundPercentage,
                Status = RefundStatus.Pending,
                Reason = booking.CancellationReason,
                RequestedAt = DateTime.UtcNow
            };

            await _refundRepository.AddAsync(refund);
            await _refundRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "INITIATE_REFUND",
                "Refund",
                refund.RefundId.ToString(),
                null,
                new { bookingId, refundAmount, refundPercentage },
                userId,
                ipAddress);

            return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(refund));
        }

     
        public async Task<ApiResponse<RefundResponseDto>> GetRefundAsync(int refundId)
        {
            var refund = await _refundRepository.GetByIdAsync(refundId);
            if (refund == null || refund.IsDeleted)
                throw new ResourceNotFoundException("Refund", refundId.ToString());

            return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(refund));
        }

   
        public async Task<ApiResponse<RefundResponseDto>> ConfirmRefundAsync(
            ConfirmRefundRequestDto dto,
            int userId,
            string ipAddress)
        {
            var refund = await _refundRepository.GetByIdAsync(dto.RefundId);
            if (refund == null || refund.IsDeleted)
                throw new ResourceNotFoundException("Refund", dto.RefundId.ToString());

            if (refund.Status != RefundStatus.Pending && refund.Status != RefundStatus.Processing)
                throw new RefundOperationException(
                    $"Cannot confirm refund with status: {refund.Status}",
                    RefundOperationException.RefundErrorType.ProcessingError);

            // DUMMY REFUND PROCESSING
            refund.ProcessedAt = DateTime.UtcNow;
            refund.Status = dto.IsApproved ? RefundStatus.Completed : RefundStatus.Rejected;
            refund.Reason = dto.Reason;

            await _refundRepository.UpdateAsync(refund);
            await _refundRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "CONFIRM_REFUND",
                "Refund",
                refund.RefundId.ToString(),
                null,
                new { bookingId = refund.BookingId, approved = dto.IsApproved, amount = refund.RefundAmount },
                userId,
                ipAddress);

            return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(refund));
        }

        /// Expire old payments (15 min timeout)
        public async Task<int> ExpireOldPaymentsAsync()
        {
            var expiredPayments = await _paymentRepository.GetExpiredPaymentsAsync();
            var now = DateTime.UtcNow;

            foreach (var payment in expiredPayments)
            {
                if (payment.Status == PaymentStatus.Pending && payment.ExpiresAt <= now)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.FailureReason = "Payment timeout - not completed within 15 minutes";
                    payment.ProcessedAt = now;

                    var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
                    if (booking != null)
                    {
                        booking.BookingStatus = BookingStatus.Expired;
                        booking.LastStatusChangeAt = now;
                        await _bookingRepository.UpdateAsync(booking);
                    }

                    await _paymentRepository.UpdateAsync(payment);
                }
            }

            await _paymentRepository.SaveChangesAsync();
            return expiredPayments.Count;
        }


        public async Task<(decimal refundAmount, int refundPercentage, decimal cancellationFee)> CalculateRefundAsync(
            int bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);
            if (schedule == null)
                throw new ResourceNotFoundException("Schedule", booking.ScheduleId.ToString());

            var departure = schedule.TravelDate.Add(schedule.DepartureTime);
            var hoursToDeparture = (int)(departure - DateTime.UtcNow).TotalHours;

            // Get applicable cancellation policy
            var policies = await _policyRepository.GetAllActiveAsync();
            var applicablePolicy = policies
                .Where(p => p.HoursBeforeDeparture <= hoursToDeparture)
                .OrderByDescending(p => p.HoursBeforeDeparture)
                .FirstOrDefault();

            if (applicablePolicy == null)
            {
                // Default: 100% refund if > 48 hrs, 75% for 24-48, 50% for 0-24, 0% after departure
                if (hoursToDeparture > 48) return (booking.TotalAmount, 100, 0);
                if (hoursToDeparture > 24) return ((booking.TotalAmount * 0.75m), 75, (booking.TotalAmount * 0.25m));
                if (hoursToDeparture > 0) return ((booking.TotalAmount * 0.5m), 50, (booking.TotalAmount * 0.5m));
                return (0, 0, booking.TotalAmount);
            }

            var refundAmount = (booking.TotalAmount * applicablePolicy.RefundPercentage) / 100;
            var cancellationFee = booking.TotalAmount - refundAmount;

            return (refundAmount, applicablePolicy.RefundPercentage, cancellationFee);
        }

        private PaymentResponseDto MapToDto(Payment p)
        {
            return new PaymentResponseDto
            {
                PaymentId = p.PaymentId,
                BookingId = p.BookingId,
                Amount = p.Amount,
                Status = p.Status,
                TransactionId = p.TransactionId,
                PaymentMethod = p.PaymentMethod,
                FailureReason = p.FailureReason,
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt,
                ExpiresAt = p.ExpiresAt
            };
        }

        private RefundResponseDto MapToDto(Refund r)
        {
            return new RefundResponseDto
            {
                RefundId = r.RefundId,
                BookingId = r.BookingId,
                RefundAmount = r.RefundAmount,
                CancellationFee = r.CancellationFee,
                RefundPercentage = r.RefundPercentage,
                Status = r.Status,
                Reason = r.Reason,
                RequestedAt = r.RequestedAt,
                ProcessedAt = r.ProcessedAt
            };
        }
    }
}
