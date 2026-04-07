using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;
using BusTicketingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            IScheduleRepository scheduleRepository,
            ISeatRepository seatRepository,
            ISeatService seatService,
            IAuditRepository auditRepository,
            IPaymentService paymentService,
            IEmailService emailService,
            IUserRepository userRepository,
            ApplicationDbContext context,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _scheduleRepository = scheduleRepository;
            _seatRepository = seatRepository;
            _auditRepository = auditRepository;
            _paymentService = paymentService;
            _emailService = emailService;
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }


        public async Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(
            CreateBookingRequestDto dto,
            int userId,
            string ipAddress)
        {
            if (dto.SeatNumbers == null || dto.SeatNumbers.Count == 0)
                throw ValidationException.ForField("seatNumbers", "At least one seat must be selected");

            if (dto.SeatNumbers.Count > 6)
                throw ValidationException.ForField("seatNumbers", "Maximum 6 seats allowed per booking");

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
                    BookingDate = DateTime.UtcNow,
                    PNR = GeneratePNR(),
                    BoardingPointName = dto.BoardingPointName,
                    DropPointName = dto.DropPointName,
                    ContactPhone = dto.ContactPhone,
                    ContactEmail = dto.ContactEmail
                };

                await _bookingRepository.AddAsync(booking);
                await _bookingRepository.SaveChangesAsync();

                // Link BookingId to seats but keep SeatStatus as "Locked"
                foreach (var seat in seats)
                {
                    seat.BookingId = booking.BookingId;
                    seat.UpdatedAt = DateTime.UtcNow;
                }
                await _seatRepository.UpdateManyAsync(seats);

                // Save passenger details if provided
                if (dto.Passengers != null && dto.Passengers.Count > 0)
                {
                    var passengers = new List<Passenger>();
                    foreach (var p in dto.Passengers)
                    {
                        var seat = seats.FirstOrDefault(s => s.SeatNumber == p.SeatNumber);
                        if (seat == null) continue;

                        // Resolve name: prefer explicit FirstName, fall back to single Name field
                        var fullName  = (p.Name ?? "").Trim();
                        var firstName = !string.IsNullOrWhiteSpace(p.FirstName)
                            ? p.FirstName.Trim()
                            : (fullName.Contains(' ') ? fullName[..fullName.IndexOf(' ')] : fullName);
                        var lastName  = !string.IsNullOrWhiteSpace(p.LastName)
                            ? p.LastName.Trim()
                            : (fullName.Contains(' ') ? fullName[(fullName.IndexOf(' ') + 1)..] : "");

                        passengers.Add(new Passenger
                        {
                            BookingId   = booking.BookingId,
                            SeatId      = seat.SeatId,
                            SeatNumber  = p.SeatNumber,
                            FirstName   = firstName,
                            LastName    = lastName,
                            Gender      = p.Gender,
                            PhoneNumber = dto.ContactPhone ?? "",
                            Email       = dto.ContactEmail ?? "",
                            Age         = p.Age,
                            CreatedAt   = DateTime.UtcNow
                        });
                    }
                    if (passengers.Count > 0)
                    {
                        await _context.Passengers.AddRangeAsync(passengers);
                    }
                }

                await _seatRepository.SaveChangesAsync();

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
            await CleanupExpiredBookingsAsync();
            await _scheduleRepository.MarkPastSchedulesInactiveAsync();

            var bookings = await _bookingRepository.GetByUserIdWithRefundAsync(userId);
            var totalCount = await _bookingRepository.GetTotalCountByUserIdAsync(userId);

            // Load all passengers for these bookings in one query to ensure Gender is fresh
            var bookingIds = bookings.Select(b => b.BookingId).ToList();
            var allPassengers = await _context.Passengers
                .Where(p => bookingIds.Contains(p.BookingId) && !p.IsDeleted)
                .ToListAsync();

            var dtos = bookings.Select(b =>
            {
                var dto = MapToDto(b);
                dto.Passengers = allPassengers
                    .Where(p => p.BookingId == b.BookingId)
                    .Select(p => new PassengerSummaryDto
                    {
                        SeatNumber = p.SeatNumber,
                        Name = $"{p.FirstName} {p.LastName}".Trim(),
                        Age = p.Age ?? 0,
                        Gender = p.Gender
                    }).ToList();
                return dto;
            }).ToList();

            var result = ApiResponse<List<BookingResponseDto>>.SuccessResponse(dtos);
            result.TotalCount = totalCount;
            return result;
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

            var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
            var bookingSeats = seats
                .Where(s => s.BookingId == booking.BookingId)
                .Select(s => s.SeatNumber)
                .OrderBy(s => s)
                .ToList();

            // Query passengers directly to ensure Gender and all fields are loaded fresh
            var passengers = await _context.Passengers
                .Where(p => p.BookingId == booking.BookingId && !p.IsDeleted)
                .ToListAsync();

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
                AvailableSeats = schedule.AvailableSeats,
                PromoCodeUsed = booking.PromoCodeUsed,
                DiscountAmount = booking.DiscountAmount,
                PNR = booking.PNR ?? string.Empty,
                SeatNumbers = bookingSeats,
                BoardingPointName = booking.BoardingPointName,
                DropPointName = booking.DropPointName,
                ContactPhone = booking.ContactPhone,
                ContactEmail = booking.ContactEmail,
                Passengers = passengers.Select(p => new PassengerSummaryDto
                {
                    SeatNumber = p.SeatNumber,
                    Name = $"{p.FirstName} {p.LastName}".Trim(),
                    Age = p.Age ?? 0,
                    Gender = p.Gender
                }).ToList(),
                Refund = booking.Refund == null ? null : new BookingRefundDto
                {
                    RefundId         = booking.Refund.RefundId,
                    RefundAmount     = booking.Refund.RefundAmount,
                    CancellationFee  = booking.Refund.CancellationFee,
                    RefundPercentage = booking.Refund.RefundPercentage,
                    Status           = booking.Refund.Status.ToString(),
                    ProcessedAt      = booking.Refund.ProcessedAt
                }
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
                    var bookedCount = affectedSeats.Count(s => s.SeatStatus == "Booked");

                    if (!wasConfirmed || role == "Admin")
                    {
                        // Release seats immediately for unpaid cancellation or admin direct cancellation
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

                            if (bookedCount > 0)
                            {
                                schedule.AvailableSeats += bookedCount;
                            }
                        }
                    }

                    if (wasConfirmed && role != "Admin")
                    {
                        booking.BookingStatus = BookingStatus.CancellationRequested;
                    }
                    else
                    {
                        booking.BookingStatus = BookingStatus.Cancelled;
                    }
                    booking.LastStatusChangeAt = DateTime.UtcNow;
                    booking.CancelledBy = role;

                    await _scheduleRepository.UpdateAsync(schedule);
                    await _bookingRepository.UpdateAsync(booking);

                    await _auditRepository.LogAuditAsync(
                        wasConfirmed && role == "Customer" ? "CANCEL_REQUEST" : "CANCEL",
                        "Booking",
                        booking.BookingId.ToString(),
                        null,
                        new { bookingId, seatsReleased = affectedSeats.Count, wasConfirmed },
                        userId,
                        ipAddress);

                    await _bookingRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Auto-create refund only for Confirmed bookings (payment was made)
                    if (wasConfirmed)
                    {
                        try
                        {
                            if (role == "Admin")
                            {
                                // Admin cancellation: 100% paid amount + 20% bonus, credited to wallet instantly
                                await _paymentService.InitiateAdminRefundAsync(bookingId, userId, ipAddress);
                            }
                            else
                            {
                                // Customer cancellation request: create refund with Pending status, no email yet
                                await _paymentService.InitiateRefundAsync(bookingId, booking.UserId, ipAddress);
                                // Note: Email will be sent after admin approval
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't un-cancel — refund failure is non-fatal
                            _logger.LogError(ex, "Refund creation failed for booking {BookingId}", bookingId);
                        }
                    }

                    // Send cancellation email to the user only for immediate cancellations
                    // (not for customer cancellation requests which need admin approval)
                    if (role == "Admin" || !wasConfirmed)
                    {
                        try
                        {
                            var user = await _userRepository.GetByIdAsync(booking.UserId);
                            if (user != null && schedule.Route != null)
                            {
                                // Fetch refund — created above, so it should exist for confirmed bookings
                                var refundResult = await _paymentService.GetRefundByBookingIdAsync(bookingId);
                                var refund = refundResult?.Data;

                                // Get the actual amount paid from the payment record
                                decimal amountPaid = 0m;
                                if (refund != null)
                                    amountPaid = refund.RefundAmount + refund.CancellationFee;

                                await _emailService.SendCancellationEmailAsync(
                                    toEmail: user.Email,
                                    userName: user.FullName,
                                    pnr: booking.PNR,
                                    source: schedule.Route.Source,
                                    destination: schedule.Route.Destination,
                                    travelDate: schedule.TravelDate,
                                    amountPaid: amountPaid,
                                    refundAmount: refund?.RefundAmount ?? 0m,
                                    refundPercentage: refund?.RefundPercentage ?? 0,
                                    cancellationFee: refund?.CancellationFee ?? 0m,
                                    cancellationReason: booking.CancellationReason ?? string.Empty);
                            }
                        }
                        catch { /* swallow — email failure should not affect cancellation result */ }
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
                CancelledBy = b.CancelledBy ?? string.Empty,
                Refund = b.Refund == null ? null : new BookingRefundDto
                {
                    RefundId = b.Refund.RefundId,
                    RefundAmount = b.Refund.RefundAmount,
                    CancellationFee = b.Refund.CancellationFee,
                    RefundPercentage = b.Refund.RefundPercentage,
                    Status = b.Refund.Status.ToString(),
                    ProcessedAt = b.Refund.ProcessedAt
                },
                Source = b.Schedule?.Route?.Source ?? string.Empty,
                Destination = b.Schedule?.Route?.Destination ?? string.Empty,
                TravelDate = b.Schedule?.TravelDate,
                DepartureTime = b.Schedule?.DepartureTime.ToString(@"hh\:mm") ?? string.Empty,
                ArrivalTime = b.Schedule?.ArrivalTime.ToString(@"hh\:mm") ?? string.Empty,
                BusNumber = b.Schedule?.Bus?.BusNumber ?? string.Empty,
                BusType = b.Schedule?.Bus?.BusType ?? string.Empty,
                OperatorName = b.Schedule?.Bus?.OperatorName ?? string.Empty,
                SeatNumbers = b.Seats?.Select(s => s.SeatNumber).OrderBy(s => s).ToList() ?? new List<string>(),
                PromoCodeUsed = b.PromoCodeUsed,
                DiscountAmount = b.DiscountAmount,
                PNR = b.PNR ?? string.Empty,
                HasRated = b.BusRating != null,
                BoardingPointName = b.BoardingPointName,
                DropPointName = b.DropPointName,
                ContactPhone = b.ContactPhone,
                ContactEmail = b.ContactEmail,
                Passengers = b.Passengers?.Select(p => new PassengerSummaryDto
                {
                    SeatNumber = p.SeatNumber,
                    Name = $"{p.FirstName} {p.LastName}".Trim(),
                    Age = p.Age ?? 0,
                    Gender = p.Gender
                }).ToList() ?? new List<PassengerSummaryDto>()
            };
        }

        public async Task<ApiResponse<bool>> RateBookingAsync(int bookingId, int userId, int rating)
        {
            if (rating < 1 || rating > 5)
                throw ValidationException.ForField("rating", "Rating must be between 1 and 5");

            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", bookingId.ToString());

            if (booking.UserId != userId)
                throw new BookingOperationException(
                    "You are not authorized to rate this booking.",
                    BookingOperationException.BookingErrorType.InvalidBooking);

            if (booking.BookingStatus != BookingStatus.Confirmed)
                throw new BookingOperationException(
                    "Only confirmed bookings can be rated.",
                    BookingOperationException.BookingErrorType.InvalidBookingStatus);

            // Prevent duplicate ratings
            var existing = await _context.BusRatings
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);
            if (existing != null)
                throw new BookingOperationException(
                    "You have already rated this booking.",
                    BookingOperationException.BookingErrorType.InvalidBooking);

            var schedule = await _scheduleRepository.GetByIdAsync(booking.ScheduleId);
            if (schedule?.Bus == null)
                throw new ResourceNotFoundException("Bus", "for this booking");

            // Save the rating
            var busRating = new BusRating
            {
                BookingId = bookingId,
                BusId = schedule.Bus.BusId,
                UserId = userId,
                Rating = rating,
                CreatedAt = DateTime.UtcNow
            };
            await _context.BusRatings.AddAsync(busRating);
            await _context.SaveChangesAsync();

            // Recalculate average from all ratings for this bus
            var avg = await _context.BusRatings
                .Where(r => r.BusId == schedule.Bus.BusId)
                .AverageAsync(r => (double)r.Rating);

            schedule.Bus.RatingAverage = Math.Round(avg, 1);
            schedule.Bus.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true);
        }

        private static string GeneratePNR()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            return new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }
    }
}
