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
        private readonly ISeatService _seatService;
        private readonly IAuditRepository _auditRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IScheduleRepository scheduleRepository,
            ISeatRepository seatRepository,
            ISeatService seatService,
            IAuditRepository auditRepository)
        {
            _bookingRepository = bookingRepository;
            _scheduleRepository = scheduleRepository;
            _seatRepository = seatRepository;
            _seatService = seatService;
            _auditRepository = auditRepository;
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

                decimal seatPrice = 500; 
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
                
                await _seatService.ConfirmBookingSeatsAsync(
                    booking.BookingId,
                    dto.ScheduleId,
                    dto.SeatNumbers,
                    userId);

                schedule.AvailableSeats -= dto.SeatNumbers.Count;
                await _scheduleRepository.UpdateAsync(schedule);
                await _scheduleRepository.SaveChangesAsync();

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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ApiResponse<List<BookingResponseDto>>>
            GetMyBookingsAsync(int userId)
        {
            var bookings = await _bookingRepository.GetByUserIdAsync(userId);

            return ApiResponse<List<BookingResponseDto>>
                .SuccessResponse(bookings.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<BookingResponseDto>>>
            GetAllBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();

            return ApiResponse<List<BookingResponseDto>>
                .SuccessResponse(bookings.Select(MapToDto).ToList());
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
                throw new Exception("Booking not found.");

            if (role == "User" && booking.UserId != userId)
                throw new Exception("Unauthorized access.");

            if (booking.BookingStatus == BookingStatus.Cancelled)
                throw new Exception("Booking already cancelled.");

            var schedule = await _scheduleRepository
                .GetByIdAsync(booking.ScheduleId);

            if (schedule == null)
                throw new Exception("Schedule not found.");

            DateTime departureDateTime =
                schedule.TravelDate.Add(schedule.DepartureTime);

            if (departureDateTime <= DateTime.UtcNow)
                throw new Exception("Cannot cancel after departure.");

            using (var transaction = await _scheduleRepository.BeginTransactionAsync())
            {
                try
                {
                    var seats = await _seatRepository.GetSeatsByScheduleIdAsync(booking.ScheduleId);
                    var bookedSeats = seats.Where(s => s.BookingId == bookingId && s.SeatStatus == "Booked").ToList();

                    if (bookedSeats.Count > 0)
                    {
                        var seatNumbers = bookedSeats.Select(s => s.SeatNumber).ToList();

                        // Release booked seats back to available
                        await _seatService.ReleaseBookingSeatsAsync(booking.ScheduleId, seatNumbers);

                        // Restore seats to schedule
                        schedule.AvailableSeats += bookedSeats.Count;
                    }

                    // Update booking status
                    booking.BookingStatus = BookingStatus.Cancelled;
                    booking.LastStatusChangeAt = DateTime.UtcNow;

                    await _scheduleRepository.UpdateAsync(schedule);
                    await _bookingRepository.UpdateAsync(booking);
                    
                    await _auditRepository.LogAuditAsync(
                        "CANCEL",
                        "Booking",
                        booking.BookingId.ToString(),
                        null,
                        new { bookingId, seatsReleased = bookedSeats.Count },
                        userId,
                        ipAddress);

                    await _bookingRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ApiResponse<bool>.SuccessResponse(true);
                }
                catch (Exception ex)
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
                BookingDate = b.BookingDate
            };
        }
    }
}
