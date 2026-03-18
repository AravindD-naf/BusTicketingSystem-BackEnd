using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using UnauthorizedAccessException = BusTicketingSystem.Exceptions.UnauthorizedAccessException;

namespace BusTicketingSystem.Services
{

    public class PassengerService : IPassengerService
    {
        private readonly IPassengerRepository _passengerRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IAuditRepository _auditRepository;

        public PassengerService(
            IPassengerRepository passengerRepository,
            IBookingRepository bookingRepository,
            ISeatRepository seatRepository,
            IAuditRepository auditRepository)
        {
            _passengerRepository = passengerRepository;
            _bookingRepository = bookingRepository;
            _seatRepository = seatRepository;
            _auditRepository = auditRepository;
        }

    
        public async Task<ApiResponse<List<PassengerResponseDto>>> AddPassengersAsync(
            AddPassengerRequestDto dto,
            int userId,
            string ipAddress)
        {
            var booking = await _bookingRepository.GetByIdAsync(dto.BookingId);
            if (booking == null || booking.IsDeleted)
                throw new ResourceNotFoundException("Booking", dto.BookingId.ToString());

            if (booking.UserId != userId)
                throw new UnauthorizedAccessException("You can only add passengers to your own booking");

            if (dto.Passengers.Count != booking.NumberOfSeats)
                throw new ValidationException(
                    $"Number of passengers must match number of seats")
                    .AddError("passengers", $"Expected {booking.NumberOfSeats} passengers, got {dto.Passengers.Count}");

            var passengers = new List<Passenger>();

            foreach (var passengerDto in dto.Passengers)
            {
                // Validate seat exists
                var seat = await _seatRepository.GetSeatByScheduleAndNumberAsync(
                    booking.ScheduleId,
                    passengerDto.SeatNumber);

                if (seat == null)
                    throw new SeatNotFoundException($"Seat {passengerDto.SeatNumber} not found.");

                if (seat.BookingId != booking.BookingId)
                    throw new InvalidSeatStatusException(
                        $"Seat {passengerDto.SeatNumber} is not assigned to this booking.");

                // Validate passenger details
                if (string.IsNullOrWhiteSpace(passengerDto.FirstName))
                    throw new InvalidPassengerException("First name is required.");

                if (string.IsNullOrWhiteSpace(passengerDto.LastName))
                    throw new InvalidPassengerException("Last name is required.");

                if (string.IsNullOrWhiteSpace(passengerDto.Email))
                    throw new InvalidPassengerException("Email is required.");

                if (string.IsNullOrWhiteSpace(passengerDto.PhoneNumber))
                    throw new InvalidPassengerException("Phone number is required.");

                var passenger = new Passenger
                {
                    BookingId = dto.BookingId,
                    SeatId = seat.SeatId,
                    SeatNumber = passengerDto.SeatNumber,
                    FirstName = passengerDto.FirstName.Trim(),
                    LastName = passengerDto.LastName.Trim(),
                    PhoneNumber = passengerDto.PhoneNumber.Trim(),
                    Email = passengerDto.Email.Trim().ToLower(),
                    IdType = passengerDto.IdType,
                    IdNumber = passengerDto.IdNumber,
                    Age = passengerDto.Age,
                    SpecialRequirements = passengerDto.SpecialRequirements
                };

                passengers.Add(passenger);
            }

            // Delete existing passengers for this booking
            var existingPassengers = await _passengerRepository.GetByBookingIdAsync(dto.BookingId);
            foreach (var existing in existingPassengers)
            {
                await _passengerRepository.DeleteAsync(existing.PassengerId);
            }

            // Add new passengers
            await _passengerRepository.AddManyAsync(passengers);
            await _passengerRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "ADD_PASSENGERS",
                "Passenger",
                dto.BookingId.ToString(),
                null,
                new { bookingId = dto.BookingId, passengerCount = passengers.Count },
                userId,
                ipAddress);

            return ApiResponse<List<PassengerResponseDto>>.SuccessResponse(
                passengers.Select(MapToDto).ToList());
        }

  
        public async Task<ApiResponse<List<PassengerResponseDto>>> GetBookingPassengersAsync(int bookingId)
        {
            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
            return ApiResponse<List<PassengerResponseDto>>.SuccessResponse(
                passengers.Select(MapToDto).ToList());
        }

  
        public async Task<ApiResponse<PassengerResponseDto>> UpdatePassengerAsync(
            int passengerId,
            PassengerDetailDto dto,
            int userId,
            string ipAddress)
        {
            throw new NotImplementedException();
        }

   
        public async Task<ApiResponse<bool>> ValidatePassengersAsync(int bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
                throw new BookingNotFoundException("Booking not found.");

            var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);

            if (passengers.Count != booking.NumberOfSeats)
                throw new InvalidPassengerException(
                    $"Missing passenger details. Expected {booking.NumberOfSeats}, got {passengers.Count}");

            return ApiResponse<bool>.SuccessResponse(true);
        }

        private PassengerResponseDto MapToDto(Passenger p)
        {
            return new PassengerResponseDto
            {
                PassengerId = p.PassengerId,
                SeatNumber = p.SeatNumber,
                FirstName = p.FirstName,
                LastName = p.LastName,
                PhoneNumber = p.PhoneNumber,
                Email = p.Email,
                IdType = p.IdType,
                IdNumber = p.IdNumber,
                Age = p.Age ?? 0,
                SpecialRequirements = p.SpecialRequirements
            };
        }
    }
}
