using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;

namespace BusTicketingSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IPaymentService _paymentService;

        public BookingService(
            IBookingRepository bookingRepository,
            IScheduleRepository scheduleRepository,
            ISeatRepository seatRepository,
            ISeatService seatService,
            IAuditRepository auditRepository,
            IPaymentService paymentService)
        {
            _bookingRepository = bookingRepository;
            _scheduleRepository = scheduleRepository;
            _seatRepository = seatRepository;
            _auditRepository = auditRepository;
            _paymentService = paymentService;
        }


        public async Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(
            CreateBookingRequestDto dto,
            int userId,
            string ipAddress)
        {
            if (dto.SeatNumbers == null || dto.SeatNumbers.Count == 0)
                throw ValidationException.ForField("seatNumbers", "At least one seat must be selected");

            var schedule = await _scheduleRepository.GetByIdAsync(dto.ScheduleId);

            if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
                throw new ResourceNotFoundException("Schedule", dto.ScheduleId.ToString());

            DateTime departureDateTime =
                schedule.TravelDate.Add(schedule.DepartureTime);

            if (departureDateTime <= DateTime.UtcNow)
                throw new BookingOperationException(
                    "Cannot book after departure time",
                    BookingOperationException.BookingErrorType.BookingExpired);

            if (dto.SeatNumbers.Count > schedule.AvailableSeats)
                throw new BookingOperationException(
                    $"Not enough seats available. Only {schedule.AvailableSeats} seats remaining",
                    BookingOperationException.BookingErrorType.InsufficientSeats);

            try
            {
                var seats = await _seatRepository.GetSeatsByNumbersAsync(dto.ScheduleId, dto.SeatNumbers);

                foreach (var seatNumber in dto.SeatNumbers)
                {
                    var seat = seats.FirstOrDefault(s => s.SeatNumber == seatNumber);

                    if (seat == null)
                        throw new SeatOperationException(
                            $"Seat {seatNumber} not found",
                            SeatOperationException.SeatErrorType.InvalidSeatNumber);

                    if (seat.SeatStatus != "Locked")
                        throw new SeatOperationException(
                            $"Seat {seatNumber} is not locked. Please lock seats before booking",
                            SeatOperationException.SeatErrorType.SeatNotLocked);

                    if (seat.LockedByUserId != userId)
                        throw new SeatOperationException(
                            $"Seat {seatNumber} is locked by another user",
                            SeatOperationException.SeatErrorType.SeatNotAvailable);
                }

                decimal seatPrice = schedule.Fare > 0? schedule.Fare : (schedule.Route?.BaseFare ?? 500);
                decimal totalAmount = dto.SeatNumbers.Count * seatPrice;

                var booking = new Booking
                {
                    UserId = userId,
                    ScheduleId = dto.ScheduleId,
                    NumberOfSeats = dto.SeatNumbers.Count,
                    TotalAmount = totalAmount,
                    BookingStatus = BookingStatus.Pending,
                    BookingDate = DateTime.UtcNow
                };

                await _bookingRepository.AddAsync(booking);
                await _bookingRepository.SaveChangesAsync();

                // ── KEY CHANGE ──
                // Link BookingId to seats but keep SeatStatus as "Locked"
                // Seats only move to "Booked" after payment is confirmed
                foreach (var seat in seats)
                {
                    seat.BookingId = booking.BookingId;
                    seat.UpdatedAt = DateTime.UtcNow;
                }
                await _seatRepository.UpdateManyAsync(seats);
                await _seatRepository.SaveChangesAsync();
                // ────────────────

                // AvailableSeats count stays the same until payment is confirmed
                // so we do NOT decrement schedule.AvailableSeats here

                await _auditRepository.LogAuditAsync(
                    "CREATE",
                    "Booking",
                    booking.BookingId.ToString(),
                    null,
                    new { bookingId = booking.BookingId, seats = dto.SeatNumbers, amount = totalAmount },
                    userId,
                    ipAddress);

                return ApiResponse<BookingResponseDto>
                    .SuccessResponse(MapToDto(booking));
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<int> CleanupExpiredBookingsAsync()
        {
            var expiredBookings = await _bookingRepository.GetExpiredPendingBookingsAsync();

            if (expiredBookings.Count == 0) return 0;

            var now = DateTime.UtcNow;

            foreach (var booking in expiredBookings)
            {
                // Cancel the booking
                booking.BookingStatus = BookingStatus.Expired;
                booking.LastStatusChangeAt = now;
                booking.CancellationReason = string.Empty;

                await _bookingRepository.UpdateAsync(booking);

                // Release any seats still linked to this booking
                var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
                var linkedSeats = seats
                    .Where(s => s.BookingId == booking.BookingId &&
                                (s.SeatStatus == "Locked" || s.SeatStatus == "Booked"))
                    .ToList();

                if (linkedSeats.Count > 0)
                {
                    foreach (var seat in linkedSeats)
                    {
                        seat.SeatStatus = "Available";
                        seat.LockedByUserId = null;
                        seat.LockedAt = null;
                        seat.BookingId = null;
                        seat.UpdatedAt = now;
                    }
                    await _seatRepository.UpdateManyAsync(linkedSeats);
                }
            }

            await _bookingRepository.SaveChangesAsync();

            return expiredBookings.Count;
        }


        public async Task<ApiResponse<List<BookingResponseDto>>>
            GetMyBookingsAsync(int userId)
        {
            // Trigger cleanup so My Bookings page always shows updated statuses
            await CleanupExpiredBookingsAsync();

            var bookings = await _bookingRepository.GetByUserIdAsync(userId);

            return ApiResponse<List<BookingResponseDto>>
                .SuccessResponse(bookings.Select(MapToDto).ToList());
        }

        public async Task<(List<BookingResponseDto> items, int totalCount)> GetAllBookingsAsync(int pageNumber, int pageSize)
        {
            var (bookings, totalCount) = await _bookingRepository.GetPagedAsync(pageNumber, pageSize);
            return (bookings.Select(MapToDto).ToList(), totalCount);
        }

        public async Task<ApiResponse<BookingDetailResponseDto>> GetBookingByIdAsync(int bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);

            if (schedule == null)
                throw new ResourceNotFoundException("Schedule", booking.ScheduleId.ToString());

            if (schedule.Route == null || schedule.Bus == null)
                throw new BookingOperationException(
                    "Schedule details are incomplete",
                    BookingOperationException.BookingErrorType.InvalidSchedule);

            var bookingDetail = new BookingDetailResponseDto
            {
                BookingId = booking.BookingId,
                ScheduleId = booking.ScheduleId,
                NumberOfSeats = booking.NumberOfSeats,
                TotalAmount = booking.TotalAmount,
                BookingStatus = booking.BookingStatus.ToString(),
                BookingDate = booking.BookingDate,
                CancellationReason = booking.CancellationReason ?? string.Empty,
                CancelledBy = booking.CancelledBy ?? string.Empty,

                RouteId = schedule.Route.RouteId,
                Source = schedule.Route.Source,
                Destination = schedule.Route.Destination,
                
                BusId = schedule.Bus.BusId,
                BusNumber = schedule.Bus.BusNumber,
                BusType = schedule.Bus.BusType,
                TotalSeats = schedule.Bus.TotalSeats,
                OperatorName = schedule.Bus.OperatorName,
                RatingAverage = schedule.Bus.RatingAverage,
                
                TravelDate = schedule.TravelDate,
                DepartureTime = schedule.DepartureTime,
                ArrivalTime = schedule.ArrivalTime,
                AvailableSeats = schedule.AvailableSeats
            };

            return ApiResponse<BookingDetailResponseDto>
                .SuccessResponse(bookingDetail);
        }


        public async Task<ApiResponse<bool>> CancelBookingAsync(
            int bookingId,
            int userId,
            string role,
            string ipAddress)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            if (role == "Customer" && booking.UserId != userId)
                throw new BookingOperationException("You are not authorized to cancel this booking.", BookingOperationException.BookingErrorType.InvalidBooking);

            if (booking.BookingStatus == BookingStatus.Cancelled)
                throw new BookingOperationException("Booking is already cancelled.", BookingOperationException.BookingErrorType.InvalidBookingStatus);

            if (booking.BookingStatus == BookingStatus.Expired)
                throw new BookingOperationException("This booking has expired and cannot be cancelled.", BookingOperationException.BookingErrorType.BookingExpired);

            var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);

            if (schedule == null)
                throw new ResourceNotFoundException("Schedule", booking.ScheduleId.ToString());

            DateTime departureDateTime = schedule.TravelDate.Add(schedule.DepartureTime);

            if (departureDateTime <= DateTime.UtcNow)
                throw new BookingOperationException("Cannot cancel a booking after the departure time.", BookingOperationException.BookingErrorType.BookingExpired);

            var wasConfirmed = booking.BookingStatus == BookingStatus.Confirmed;

            using (var transaction = await _scheduleRepository.BeginTransactionAsync())
            {
                try
                {
                    var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
                    
                    // ── KEY CHANGE ── handle both Locked (pending payment) and Booked (paid)
                    var affectedSeats = seats
                        .Where(s => s.BookingId == bookingId &&
                               (s.SeatStatus == "Booked" || s.SeatStatus == "Locked"))
                        .ToList();

                    // FIX — capture count BEFORE modifying statuses
                    if (affectedSeats.Count > 0)
                    {
                        // Capture BEFORE the loop overwrites SeatStatus
                        var bookedCount = affectedSeats.Count(s => s.SeatStatus == "Booked");

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

                        // Restore AvailableSeats for confirmed (Booked) seats
                        if (bookedCount > 0)
                        {
                            schedule.AvailableSeats += bookedCount;
                        }
                    }
                    // ────────────────

                    booking.BookingStatus = BookingStatus.Cancelled;
                    booking.LastStatusChangeAt = DateTime.UtcNow;
                    booking.CancelledBy = role;

                    await _scheduleRepository.UpdateAsync(schedule);
                    await _bookingRepository.UpdateAsync(booking);

                    await _auditRepository.LogAuditAsync(
                        "CANCEL",
                        "Booking",
                        booking.BookingId.ToString(),
                        null,
                        new { bookingId, seatsReleased = affectedSeats.Count },
                        userId,
                        ipAddress);

                    await _bookingRepository.SaveChangesAsync();
                    await transaction.CommitAsync();
                    // Auto-create refund only for Confirmed bookings (payment was made)
                    if (wasConfirmed)
                    {
                        try
                        {
                            await _paymentService.InitiateRefundAsync(bookingId, userId, ipAddress);
                        }
                        catch { /* swallow — refund failure should not un-cancel the booking */ }
                    }

                    return ApiResponse<bool>.SuccessResponse(true);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private BookingResponseDto MapToDto(Booking b)
        {
            return new BookingResponseDto
            {
                BookingId = b.BookingId,
                ScheduleId = b.ScheduleId,
                NumberOfSeats = b.NumberOfSeats,
                TotalAmount = b.TotalAmount,
                BookingStatus = b.BookingStatus.ToString(),
                BookingDate = b.BookingDate,
                CancellationReason = b.CancellationReason ?? string.Empty,
                CancelledBy = b.CancelledBy ?? string.Empty
            };
        }
    }
}
