using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using BusTicketingSystem.Models.Enums;

namespace BusTicketingSystem.Services
{
    public class SeatService : ISeatService
    {
        private readonly ISeatRepository _seatRepository;
        private readonly ISeatLockRepository _seatLockRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly IBookingRepository _bookingRepository;
        private const int LOCK_EXPIRY_MINUTES = 5;

        public SeatService(
            ISeatRepository seatRepository,
            ISeatLockRepository seatLockRepository,
            IScheduleRepository scheduleRepository,
            IAuditRepository auditRepository,
            IBookingRepository bookingRepository)
        {
            _seatRepository = seatRepository;
            _seatLockRepository = seatLockRepository;
            _scheduleRepository = scheduleRepository;
            _auditRepository = auditRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<ApiResponse<SeatLayoutResponseDto>> GetSeatLayoutAsync(int scheduleId)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
            if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
                throw new ResourceNotFoundException("Schedule", scheduleId.ToString());

            // ADD THESE TWO LINES � clean up expired locks on every layout fetch
            await _seatRepository.CleanupExpiredLocksAsync();
            await _seatLockRepository.CleanupExpiredLocksAsync();
            // ?? Inline cleanup of expired pending bookings ??
            await CleanupExpiredPendingBookingsAsync();

            var seats = await _seatRepository.GetSeatsByScheduleIdAsync(scheduleId);

            var seatLayout = new SeatLayoutResponseDto
            {
                ScheduleId = scheduleId,
                BusId = schedule.BusId,
                BusNumber = schedule.Bus?.BusNumber ?? string.Empty,
                BaseFare = schedule.Fare > 0 ? schedule.Fare : (schedule.Route?.BaseFare ?? 0),
                TotalSeats = schedule.TotalSeats,
                AvailableSeats = seats.Count(s => s.SeatStatus == "Available"),
                LockedSeats = seats.Count(s => s.SeatStatus == "Locked"),
                BookedSeats = seats.Count(s => s.SeatStatus == "Booked"),
                Seats = seats.Select(s => new SeatResponseDto
                {
                    SeatId = s.SeatId,
                    SeatNumber = s.SeatNumber,
                    SeatStatus = s.SeatStatus,
                    LockedByUserId = s.LockedByUserId,
                    LockedAt = s.LockedAt,
                    LockedExpiresAt = s.LockedAt?.AddMinutes(LOCK_EXPIRY_MINUTES)
                }).ToList()
            };

            return ApiResponse<SeatLayoutResponseDto>.SuccessResponse(seatLayout);
        }


        private async Task CleanupExpiredPendingBookingsAsync()
        {
            var expiredBookings = await _bookingRepository
                .GetExpiredPendingBookingsAsync();

            if (expiredBookings.Count == 0) return;

            var now = DateTime.UtcNow;

            foreach (var booking in expiredBookings)
            {
                booking.BookingStatus = BookingStatus.Expired;
                booking.LastStatusChangeAt = now;
                booking.CancellationReason = string.Empty;

                await _bookingRepository.UpdateAsync(booking);

                // Release any seats still linked to this booking
                var seats = await _seatRepository
                    .GetSeatsByScheduleIdAsync(booking.ScheduleId);

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
        }


        public async Task<ApiResponse<LockSeatsResponseDto>> LockSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int userId,
            string ipAddress)
        {
            if (seatNumbers == null || seatNumbers.Count == 0)
                throw new ValidationException("At least one seat must be selected.", "VAL_SEAT_SELECTION");

            var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
            if (schedule == null || schedule.IsDeleted || !schedule.IsActive)
                throw new ResourceNotFoundException("Schedule", scheduleId.ToString());

            DateTime departureDateTime = schedule.TravelDate.Add(schedule.DepartureTime);
            if (departureDateTime <= DateTime.UtcNow)
                throw new BookingOperationException("Cannot lock seats after the departure time.", BookingOperationException.BookingErrorType.BookingExpired);

            var response = new LockSeatsResponseDto
            {
                LockedSeatNumbers = new List<string>(),
                FailedSeatNumbers = new List<string>()
            };

            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(LOCK_EXPIRY_MINUTES);

            await _seatLockRepository.CleanupExpiredLocksAsync();
            await _seatRepository.CleanupExpiredLocksAsync();

            var seats = await _seatRepository.GetSeatsByNumbersAsync(scheduleId, seatNumbers);

            foreach (var seatNumber in seatNumbers)
            {
                var seat = seats.FirstOrDefault(s => s.SeatNumber == seatNumber);

                if (seat == null)
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (not found)");
                    continue;
                }

                if (seat.SeatStatus == "Booked")
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (already booked)");
                    continue;
                }

                if (seat.SeatStatus == "Locked" && seat.LockedByUserId != userId)
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (locked by another user)");
                    continue;
                }

                if (seat.SeatStatus == "Locked" && seat.LockedByUserId == userId)
                {
                    // Refresh the lock timestamp
                    seat.LockedAt = now;
                    seat.UpdatedAt = now;
                    response.LockedSeatNumbers.Add(seatNumber);
                    continue;
                }

                seat.SeatStatus = "Locked";
                seat.LockedByUserId = userId;
                seat.LockedAt = now;
                seat.UpdatedAt = now;

                var seatLock = new SeatLock
                {
                    SeatId = seat.SeatId,
                    UserId = userId,
                    LockedAt = now,
                    ExpiresAt = expiresAt
                };

                await _seatLockRepository.AddAsync(seatLock);
                response.LockedSeatNumbers.Add(seatNumber);
            }

            var seatsToUpdate = seats.Where(s => response.LockedSeatNumbers.Contains(s.SeatNumber)).ToList();
            if (seatsToUpdate.Count > 0)
            {
                await _seatRepository.UpdateManyAsync(seatsToUpdate);
            }

            await _seatRepository.SaveChangesAsync();

            response.Success = response.FailedSeatNumbers.Count == 0;
            response.LockExpiresAt = expiresAt;
            response.Message = response.Success
                ? $"All {response.LockedSeatNumbers.Count} seats locked successfully."
                : $"Locked {response.LockedSeatNumbers.Count} seats. Failed: {response.FailedSeatNumbers.Count}";

            await _auditRepository.LogAuditAsync(
                "LOCK_SEATS",
                "Seat",
                string.Join(",", response.LockedSeatNumbers),
                null,
                new { scheduleId, seatNumbers = response.LockedSeatNumbers },
                userId,
                ipAddress);

            return ApiResponse<LockSeatsResponseDto>.SuccessResponse(response);
        }

        public async Task<ApiResponse<ReleaseSeatsResponseDto>> ReleaseSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int userId,
            string ipAddress)
        {
            if (seatNumbers == null || seatNumbers.Count == 0)
                throw new ValidationException("At least one seat must be specified.", "VAL_SEAT_SELECTION");

            var response = new ReleaseSeatsResponseDto
            {
                ReleasedSeatNumbers = new List<string>(),
                FailedSeatNumbers = new List<string>()
            };

            var seats = await _seatRepository.GetSeatsByNumbersAsync(scheduleId, seatNumbers);

            foreach (var seatNumber in seatNumbers)
            {
                var seat = seats.FirstOrDefault(s => s.SeatNumber == seatNumber);

                if (seat == null)
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (not found)");
                    continue;
                }

                if (seat.SeatStatus != "Locked")
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (not locked)");
                    continue;
                }

                if (seat.LockedByUserId != userId)
                {
                    response.FailedSeatNumbers.Add($"{seatNumber} (locked by another user)");
                    continue;
                }

                seat.SeatStatus = "Available";
                seat.LockedByUserId = null;
                seat.LockedAt = null;
                seat.UpdatedAt = DateTime.UtcNow;

                response.ReleasedSeatNumbers.Add(seatNumber);
            }

            var seatsToUpdate = seats.Where(s => response.ReleasedSeatNumbers.Contains(s.SeatNumber)).ToList();
            if (seatsToUpdate.Count > 0)
            {
                await _seatRepository.UpdateManyAsync(seatsToUpdate);
                await _seatRepository.SaveChangesAsync();
            }

            response.Success = response.FailedSeatNumbers.Count == 0;
            response.Message = response.Success
                ? $"All {response.ReleasedSeatNumbers.Count} seats released successfully."
                : $"Released {response.ReleasedSeatNumbers.Count} seats. Failed: {response.FailedSeatNumbers.Count}";

            await _auditRepository.LogAuditAsync(
                "RELEASE_SEATS",
                "Seat",
                string.Join(",", response.ReleasedSeatNumbers),
                null,
                new { scheduleId, seatNumbers = response.ReleasedSeatNumbers },
                userId,
                ipAddress);

            return ApiResponse<ReleaseSeatsResponseDto>.SuccessResponse(response);
        }

        public async Task<int> CleanupExpiredLocksAsync()
        {
            return await _seatLockRepository.CleanupExpiredLocksAsync();
        }

        public async Task<int> ExtendSeatsLockAsync(int scheduleId, int userId, int extendByMinutes)
        {
            // Extend both the Seat.LockedAt timestamp and the SeatLock.ExpiresAt
            var seatsExtended = await _seatRepository.ExtendLocksForUserAsync(scheduleId, userId, extendByMinutes);
            await _seatLockRepository.ExtendUserLocksAsync(scheduleId, userId, extendByMinutes);
            return seatsExtended;
        }

        public async Task<ApiResponse<bool>> ConfirmBookingSeatsAsync(
            int bookingId,
            int scheduleId,
            List<string> seatNumbers,
            int userId)
        {
            if (seatNumbers == null || seatNumbers.Count == 0)
                throw new ValidationException("At least one seat must be confirmed.", "VAL_SEAT_CONFIRM");

            var seats = await _seatRepository.GetSeatsByNumbersAsync(scheduleId, seatNumbers);
            var seatsToConfirm = new List<Seat>();
            var now = DateTime.UtcNow;

            foreach (var seatNumber in seatNumbers)
            {
                var seat = seats.FirstOrDefault(s => s.SeatNumber == seatNumber);

                if (seat == null)
                    throw new SeatOperationException($"Seat {seatNumber} not found.", SeatOperationException.SeatErrorType.InvalidSeatNumber);

                if (seat.SeatStatus != "Locked")
                    throw new SeatOperationException($"Seat {seatNumber} is not locked.", SeatOperationException.SeatErrorType.SeatNotLocked);

                if (seat.LockedByUserId != userId)
                    throw new SeatOperationException($"Seat {seatNumber} is locked by another user.", SeatOperationException.SeatErrorType.SeatNotAvailable);

                seat.SeatStatus = "Booked";
                seat.BookingId = bookingId;
                seat.LockedByUserId = null;
                seat.LockedAt = null;
                seat.UpdatedAt = now;
                seatsToConfirm.Add(seat);
            }

            // BUG FIX: only update the confirmed seats, not the entire fetched list
            if (seatsToConfirm.Count > 0)
            {
                await _seatRepository.UpdateManyAsync(seatsToConfirm);
                await _seatRepository.SaveChangesAsync();
            }

            return ApiResponse<bool>.SuccessResponse(true);
        }

        public async Task<ApiResponse<bool>> ReleaseBookingSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int bookingId)
        {
            if (seatNumbers == null || seatNumbers.Count == 0)
                throw new ValidationException("At least one seat must be specified.", "VAL_SEAT_SELECTION");

            var seats = await _seatRepository.GetSeatsByNumbersAsync(scheduleId, seatNumbers);
            var seatsToRelease = new List<Seat>();
            var now = DateTime.UtcNow;

            foreach (var seatNumber in seatNumbers)
            {
                var seat = seats.FirstOrDefault(s => s.SeatNumber == seatNumber);

                if (seat == null)
                    throw new SeatOperationException($"Seat {seatNumber} not found.", SeatOperationException.SeatErrorType.InvalidSeatNumber);

                // BUG FIX: verify the seat actually belongs to this booking before releasing
                if (seat.BookingId != bookingId)
                    throw new SeatOperationException(
                        $"Seat {seatNumber} does not belong to booking {bookingId}.",
                        SeatOperationException.SeatErrorType.SeatNotAvailable);

                if (seat.SeatStatus != "Booked")
                    throw new SeatOperationException($"Seat {seatNumber} is not booked.", SeatOperationException.SeatErrorType.SeatNotAvailable);

                seat.SeatStatus = "Available";
                seat.BookingId = null;
                seat.UpdatedAt = now;
                seatsToRelease.Add(seat);
            }

            if (seatsToRelease.Count > 0)
            {
                await _seatRepository.UpdateManyAsync(seatsToRelease);
                await _seatRepository.SaveChangesAsync();
            }

            return ApiResponse<bool>.SuccessResponse(true);
        }
    }
}
