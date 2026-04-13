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
        private readonly ISeatRepository _seatRepository;
        private readonly IPromoCodeService _promoCodeService;
        private readonly IWalletService _walletService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IRefundRepository refundRepository,
            IBookingRepository bookingRepository,
            IScheduleRepository scheduleRepository,
            ICancellationPolicyRepository policyRepository,
            IAuditRepository auditRepository,
            ISeatRepository seatRepository,
            IPromoCodeService promoCodeService,
            IWalletService walletService,
            IEmailService emailService,
            IUserRepository userRepository)
        {
            _paymentRepository = paymentRepository;
            _refundRepository = refundRepository;
            _bookingRepository = bookingRepository;
            _scheduleRepository = scheduleRepository;
            _policyRepository = policyRepository;
            _auditRepository = auditRepository;
            _seatRepository = seatRepository;
            _promoCodeService = promoCodeService;
            _walletService = walletService;
            _emailService = emailService;
            _userRepository = userRepository;
        }

        public async Task<ApiResponse<PaymentResponseDto>> InitiatePaymentAsync(
            int bookingId,
            decimal amount,
            string paymentMethod,
            int userId,
            string ipAddress,
            string? promoCode = null
            )
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("You cannot initiate payment for this booking.");

            if (booking.BookingStatus != BookingStatus.Pending &&
                booking.BookingStatus != BookingStatus.PaymentFailed &&
                booking.BookingStatus != BookingStatus.PaymentProcessing)
                throw new PaymentOperationException(
                    "Cannot initiate payment for this booking",
                    PaymentOperationException.PaymentErrorType.ProcessingError);

            // Reset status back to Pending so the flow works cleanly
            if (booking.BookingStatus != BookingStatus.Pending)
            {
                booking.BookingStatus = BookingStatus.Pending;
                await _bookingRepository.UpdateAsync(booking);
            }

            // Apply promo code discount if provided
            decimal discountAmount = booking.DiscountAmount; // use already-stored discount by default
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var promoResult = await _promoCodeService.ValidateAsync(promoCode, booking.TotalAmount);
                if (!promoResult.IsValid)
                    throw new PaymentOperationException(
                        promoResult.Message,
                        PaymentOperationException.PaymentErrorType.ProcessingError);

                discountAmount = promoResult.DiscountAmount;
                booking.PromoCodeUsed = promoResult.Code;
                booking.DiscountAmount = discountAmount;
                await _bookingRepository.UpdateAsync(booking);
                await _promoCodeService.IncrementUsageAsync(promoResult.Code);
            }

            decimal finalAmount = booking.TotalAmount - discountAmount;

            // Compute expected grand total (base fare - discount + 6% tax + â‚¹20 convenience fee)
            // and validate against what the frontend sent
            decimal tax = Math.Round(finalAmount * 0.06m);
            const decimal convenienceFee = 20m;
            decimal expectedGrandTotal = finalAmount + tax + convenienceFee;

            if (Math.Abs(amount - expectedGrandTotal) > 1m)
                throw new PaymentOperationException(
                    "Payment amount does not match booking total after discount",
                    PaymentOperationException.PaymentErrorType.InvalidAmount);

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = expectedGrandTotal,
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
                new { bookingId, amount = expectedGrandTotal, paymentMethod, promoCode, discountAmount },
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

                // ?? KEY CHANGE ?? Confirm seats: Locked ? Booked (only now, after payment)
                var lockedSeats = await _seatRepository.GetLockedSeatsByUserAsync(
                    booking.ScheduleId, booking.UserId);

                var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);

                if (lockedSeats.Count > 0)
                {
                    var now = DateTime.UtcNow;
                    foreach (var seat in lockedSeats)
                    {
                        seat.SeatStatus = "Booked";
                        // BUG FIX: BookingId was never set here — confirmed seats had null BookingId
                        seat.BookingId = booking.BookingId;
                        seat.LockedByUserId = null;
                        seat.LockedAt = null;
                        seat.UpdatedAt = now;
                    }
                    await _seatRepository.UpdateManyAsync(lockedSeats);

                    // Now decrement AvailableSeats since payment is confirmed
                    if (schedule != null)
                    {
                        schedule.AvailableSeats -= lockedSeats.Count;
                        await _scheduleRepository.UpdateAsync(schedule);
                    }
                }
                // Send booking confirmation email
                var user = await _userRepository.GetByIdAsync(booking.UserId);
                if (user != null && schedule != null)
                {
                    // Grand total = what Razorpay/wallet actually charged, stored on payment.Amount
                    decimal emailBaseFare      = booking.TotalAmount;
                    decimal emailDiscount      = booking.DiscountAmount;
                    decimal emailFareAfterDisc = emailBaseFare - emailDiscount;
                    decimal emailGst           = Math.Round(emailFareAfterDisc * 0.06m, 2);
                    const decimal emailConvFee = 20m;
                    // Use payment.Amount as the authoritative grand total (what was actually charged)
                    decimal emailGrandTotal    = payment.Amount > 0
                        ? payment.Amount
                        : emailFareAfterDisc + emailGst + emailConvFee;

                    // Collect seat numbers from the seats that were just confirmed
                    var emailSeatNumbers = lockedSeats
                        .Select(s => s.SeatNumber)
                        .OrderBy(s => s)
                        .ToList();

                    await _emailService.SendBookingConfirmationAsync(
                        toEmail: user.Email,
                        userName: user.FullName,
                        pnr: booking.PNR,
                        source: schedule.Route?.Source ?? string.Empty,
                        destination: schedule.Route?.Destination ?? string.Empty,
                        travelDate: schedule.TravelDate,
                        departureTime: schedule.DepartureTime.ToString(@"hh\:mm"),
                        arrivalTime: schedule.ArrivalTime.ToString(@"hh\:mm"),
                        numberOfSeats: booking.NumberOfSeats,
                        seatNumbers: emailSeatNumbers,
                        baseFare: emailBaseFare,
                        discountAmount: emailDiscount,
                        gstAmount: emailGst,
                        convenienceFee: emailConvFee,
                        grandTotal: emailGrandTotal,
                        promoCode: booking.PromoCodeUsed);
                }
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = dto.FailureReason;
                booking.BookingStatus = BookingStatus.PaymentFailed;
                booking.LastStatusChangeAt = DateTime.UtcNow;

                // ?? KEY CHANGE ?? Payment failed: release locked seats back to Available
                var lockedSeats = await _seatRepository.GetLockedSeatsByUserAsync(
                    booking.ScheduleId, booking.UserId);

                if (lockedSeats.Count > 0)
                {
                    var now = DateTime.UtcNow;
                    foreach (var seat in lockedSeats)
                    {
                        seat.SeatStatus = "Available";
                        seat.LockedByUserId = null;
                        seat.LockedAt = null;
                        seat.BookingId = null;
                        seat.UpdatedAt = now;
                    }
                    await _seatRepository.UpdateManyAsync(lockedSeats);
                }
                // ????????????????
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

            // Fetch payment directly — do not rely on booking navigation property
            var payment = await _paymentRepository.GetByBookingIdAsync(bookingId);
            if (payment == null || payment.Status != PaymentStatus.Success)
                throw new RefundOperationException(
                    $"Cannot refund booking {bookingId}: payment not found or not successful (status: {payment?.Status})",
                    RefundOperationException.RefundErrorType.InvalidRefund);

            var existingRefund = await _refundRepository.GetByBookingIdAsync(bookingId);
            if (existingRefund != null)
                return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(existingRefund));

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
                "INITIATE_REFUND", "Refund", refund.RefundId.ToString(), null,
                new { bookingId, refundAmount, refundPercentage }, userId, ipAddress);

            return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(refund));
        }

        /// <summary>
        /// Admin-initiated refund: 100% of amount paid + 20% bonus, auto-approved and
        /// credited instantly to the customer's wallet.
        /// </summary>
        public async Task<ApiResponse<RefundResponseDto>> InitiateAdminRefundAsync(
            int bookingId,
            int adminUserId,
            string ipAddress)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            var payment = await _paymentRepository.GetByBookingIdAsync(bookingId);
            if (payment == null || payment.Status != PaymentStatus.Success)
                // No payment was made â€” nothing to refund
                return ApiResponse<RefundResponseDto>.SuccessResponse((RefundResponseDto)null!);

            var existingRefund = await _refundRepository.GetByBookingIdAsync(bookingId);
            if (existingRefund != null)
                return ApiResponse<RefundResponseDto>.SuccessResponse(MapToDto(existingRefund));

            // 100% of what the customer actually paid + 20% bonus
            decimal amountPaid    = payment.Amount;
            decimal bonus         = Math.Round(amountPaid * 0.20m, 2);
            decimal totalRefund   = amountPaid + bonus;

            var refund = new Refund
            {
                BookingId       = bookingId,
                PaymentId       = payment.PaymentId,
                RefundAmount    = totalRefund,
                CancellationFee = 0,
                RefundPercentage = 120, // 100% + 20% bonus
                Status          = RefundStatus.Completed, // auto-approved
                Reason          = "Booking cancelled by Admin â€” full refund + 20% compensation",
                RequestedAt     = DateTime.UtcNow,
                ProcessedAt     = DateTime.UtcNow
            };

            await _refundRepository.AddAsync(refund);
            await _refundRepository.SaveChangesAsync();

            // Credit wallet immediately
            try
            {
                await _walletService.CreditAsync(
                    booking.UserId,
                    totalRefund,
                    $"Admin cancellation refund for Booking #{bookingId} (100% + 20% bonus)",
                    bookingId.ToString(),
                    ipAddress);
            }
            catch { /* swallow â€” refund record is saved; wallet credit can be retried */ }

            await _auditRepository.LogAuditAsync(
                "ADMIN_REFUND", "Refund", refund.RefundId.ToString(), null,
                new { bookingId, amountPaid, bonus, totalRefund, creditedToWallet = true },
                adminUserId, ipAddress);

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

            // Credit wallet if approved — refund goes directly to user's wallet
            refund.ProcessedAt = DateTime.UtcNow;
            refund.Status = dto.IsApproved ? RefundStatus.Completed : RefundStatus.Rejected;
            refund.Reason = dto.Reason;

            var booking = await _bookingRepository.GetByIdAsync(refund.BookingId);
            if (booking != null)
            {
                if (dto.IsApproved && booking.BookingStatus == BookingStatus.CancellationRequested)
                {
                    // Approve cancellation and release seats
                    booking.BookingStatus = BookingStatus.Cancelled;
                    booking.LastStatusChangeAt = DateTime.UtcNow;
                    await _bookingRepository.UpdateAsync(booking);

                    var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
                    var affectedSeats = seats
                        .Where(s => s.BookingId == refund.BookingId && s.SeatStatus == "Booked")
                        .ToList();

                    if (affectedSeats.Count > 0)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var seat in affectedSeats)
                        {
                            seat.SeatStatus = "Available";
                            seat.LockedByUserId = null;
                            seat.LockedAt = null;
                            seat.BookingId = null;
                            seat.UpdatedAt = now;
                        }
                        await _seatRepository.UpdateManyAsync(affectedSeats);

                        var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);
                        if (schedule != null)
                        {
                            schedule.AvailableSeats += affectedSeats.Count;
                            await _scheduleRepository.UpdateAsync(schedule);
                        }
                    }
                }
                else if (!dto.IsApproved && booking.BookingStatus == BookingStatus.CancellationRequested)
                {
                    // Rejection: cancellation still goes through — booking is Cancelled,
                    // but no refund is credited to the user's wallet.
                    booking.BookingStatus = BookingStatus.Cancelled;
                    booking.LastStatusChangeAt = DateTime.UtcNow;
                    await _bookingRepository.UpdateAsync(booking);

                    // Release the seats since the booking is now fully cancelled
                    var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
                    var affectedSeats = seats
                        .Where(s => s.BookingId == refund.BookingId && s.SeatStatus == "Booked")
                        .ToList();

                    if (affectedSeats.Count > 0)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var seat in affectedSeats)
                        {
                            seat.SeatStatus = "Available";
                            seat.LockedByUserId = null;
                            seat.LockedAt = null;
                            seat.BookingId = null;
                            seat.UpdatedAt = now;
                        }
                        await _seatRepository.UpdateManyAsync(affectedSeats);

                        var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);
                        if (schedule != null)
                        {
                            schedule.AvailableSeats += affectedSeats.Count;
                            await _scheduleRepository.UpdateAsync(schedule);
                        }
                    }
                }
            }

            await _refundRepository.SaveChangesAsync();

            if (dto.IsApproved && refund.RefundAmount > 0)
            {
                if (booking != null)
                {
                    try
                    {
                        await _walletService.CreditAsync(
                            booking.UserId,
                            refund.RefundAmount,
                            $"Refund for Booking #{refund.BookingId}",
                            refund.BookingId.ToString(),
                            ipAddress);
                    }
                    catch { /* swallow — refund record is already saved */ }
                }
            }

            await _auditRepository.LogAuditAsync(
                "CONFIRM_REFUND",
                "Refund",
                refund.RefundId.ToString(),
                null,
                new { bookingId = refund.BookingId, approved = dto.IsApproved, amount = refund.RefundAmount },
                userId,
                ipAddress);

            // Send email notification after admin decision
            try
            {
                var user = booking != null ? await _userRepository.GetByIdAsync(booking.UserId) : null;
                if (user != null && booking != null)
                {
                    var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);
                    var source = schedule?.Route?.Source ?? string.Empty;
                    var destination = schedule?.Route?.Destination ?? string.Empty;
                    var travelDate = schedule?.TravelDate ?? DateTime.UtcNow;

                    if (dto.IsApproved)
                    {
                        await _emailService.SendCancellationEmailAsync(
                            toEmail: user.Email,
                            userName: user.FullName,
                            pnr: booking.PNR,
                            source: source,
                            destination: destination,
                            travelDate: travelDate,
                            amountPaid: refund.RefundAmount + refund.CancellationFee,
                            refundAmount: refund.RefundAmount,
                            refundPercentage: refund.RefundPercentage,
                            cancellationFee: refund.CancellationFee,
                            cancellationReason: booking.CancellationReason ?? string.Empty);
                    }
                    else
                    {
                        await _emailService.SendRefundStatusEmailAsync(
                            toEmail: user.Email,
                            userName: user.FullName,
                            pnr: booking.PNR,
                            isApproved: false,
                            refundAmount: refund.RefundAmount,
                            reason: dto.Reason ?? string.Empty);
                    }
                }
            }
            catch { /* swallow — email failure should not affect refund result */ }

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

        public async Task<ApiResponse<RefundResponseDto?>> GetRefundByBookingIdAsync(int bookingId)
        {
            var refund = await _refundRepository.GetByBookingIdAsync(bookingId);
            if (refund == null) return ApiResponse<RefundResponseDto?>.SuccessResponse((RefundResponseDto?)null);
            return ApiResponse<RefundResponseDto?>.SuccessResponse(MapToDto(refund));
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

            // Use the actual amount paid (includes GST + convenience fee) for refund calculation
            var payment = await _paymentRepository.GetByBookingIdAsync(bookingId);
            decimal amountPaid = (payment != null && payment.Status == PaymentStatus.Success)
                ? payment.Amount
                : booking.TotalAmount;

            var departure = schedule.TravelDate.Add(schedule.DepartureTime);
            // Use double for precision; negative means departure already passed
            double hoursToDepartureDouble = (departure - DateTime.UtcNow).TotalHours;
            int hoursToDeparture = (int)Math.Floor(hoursToDepartureDouble);

            // Past departure — no refund
            if (hoursToDeparture < 0)
                return (0, 0, amountPaid);

            // Get applicable cancellation policy
            // Policy with the highest HoursBeforeDeparture that is <= current hours remaining
            var policies = await _policyRepository.GetAllActiveAsync();
            var applicablePolicy = policies
                .Where(p => p.HoursBeforeDeparture <= hoursToDeparture)
                .OrderByDescending(p => p.HoursBeforeDeparture)
                .FirstOrDefault();

            if (applicablePolicy == null)
            {
                // Default policy when no custom policy is configured
                if (hoursToDeparture > 48) return (amountPaid, 100, 0);
                if (hoursToDeparture > 24) return (Math.Round(amountPaid * 0.75m, 2), 75, Math.Round(amountPaid * 0.25m, 2));
                if (hoursToDeparture > 0)  return (Math.Round(amountPaid * 0.50m, 2), 50, Math.Round(amountPaid * 0.50m, 2));
                return (0, 0, amountPaid);
            }

            var refundAmount    = Math.Round((amountPaid * applicablePolicy.RefundPercentage) / 100, 2);
            var cancellationFee = Math.Round(amountPaid - refundAmount, 2);

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
