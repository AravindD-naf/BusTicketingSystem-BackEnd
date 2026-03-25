using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using System.Net;
using System.Security.Claims;
using UnauthorizedAccessException = BusTicketingSystem.Exceptions.UnauthorizedAccessException;

namespace BusTicketingSystem.Services
{
    public class BusService : IBusService
    {
        private readonly IBusRepository _busRepository;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IScheduleRepository _scheduleRepository;


        public BusService(IBusRepository busRepository, IAuditService auditService,
    IHttpContextAccessor httpContextAccessor, IScheduleRepository scheduleRepository)
        {
            _busRepository = busRepository;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _scheduleRepository = scheduleRepository;
        }

        public async Task<BusResponse> CreateBusAsync(
            CreateBusRequest request,
            int userId,
            string? ipAddress)
        {
            var normalizedBusNumber = request.BusNumber.Trim().ToUpper();

            var existing = await _busRepository.GetByBusNumberAsync(normalizedBusNumber);
            if (existing != null && !existing.IsDeleted)
                throw new ConflictException($"Bus number {normalizedBusNumber} already exists");

            var bus = new Bus
            {
                BusNumber = normalizedBusNumber,
                BusType = request.BusType,
                TotalSeats = request.TotalSeats,
                OperatorName = request.OperatorName.Trim(),
                IsActive = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _busRepository.CreateAsync(bus);

            await _auditService.LogAsync(
                userId,
                "Create",
                "Bus",
                bus.BusId.ToString(),
                null,
                new
                {
                    bus.BusNumber,
                    bus.BusType,
                    bus.TotalSeats,
                    bus.OperatorName,
                    bus.IsActive
                },
                ipAddress);

            return MapToResponse(bus);
        }

        public async Task<(List<BusResponse> items, int totalCount)> GetAllBusesAsync(int pageNumber, int pageSize)
        {
            var (buses, totalCount) = await _busRepository.GetPagedAsync(pageNumber, pageSize);
            return (buses.Select(MapToResponse).ToList(), totalCount);
        }

        public async Task<BusResponse> GetBusByIdAsync(int id)
        {
            var bus = await _busRepository.GetByIdAsync(id);
            if (bus == null)
                throw new NotFoundException("Bus not found.");

            return MapToResponse(bus);
        }

        public async Task UpdateBusAsync(
            int id,
            UpdateBusRequest request,
            int userId,
            string? ipAddress)
        {
            var bus = await _busRepository.GetByIdAsync(id);

            if (bus == null || bus.IsDeleted)
                throw new NotFoundException("Bus not found.");

            var hasSchedule = await _scheduleRepository.HasFutureSchedulesForBusAsync(id);
            if (hasSchedule)
                throw new ConflictException(
                    "This bus has upcoming schedules and cannot be edited. " +
                    "Please remove all future schedules before making changes.");

            var oldValues = new
            {
                bus.BusNumber,
                bus.BusType,
                bus.TotalSeats,
                bus.OperatorName,
                bus.IsActive
            };

            bus.BusType = request.BusType;
            bus.TotalSeats = request.TotalSeats;
            bus.OperatorName = request.OperatorName.Trim();
            if (request.IsActive.HasValue)
            {
                bus.IsActive = request.IsActive.Value;
            }
            //bus.IsActive = request.IsActive;
            bus.UpdatedAt = DateTime.UtcNow;

            await _busRepository.UpdateAsync(bus);

            var newValues = new
            {
                bus.BusNumber,
                bus.BusType,
                bus.TotalSeats,
                bus.OperatorName,
                bus.IsActive
            };

            await _auditService.LogAsync(
                userId,
                "Update",
                "Bus",
                bus.BusId.ToString(),
                oldValues,
                newValues,
                ipAddress);
        }

        public async Task DeleteBusAsync(int id)
        {
            var bus = await _busRepository.GetByIdAsync(id);
            if (bus == null)
                throw new NotFoundException("Bus not found.");

            var hasSchedule = await _scheduleRepository.HasFutureSchedulesForBusAsync(id);
            if (hasSchedule)
                throw new ConflictException(
                    "This bus has upcoming schedules and cannot be deleted. " +
                    "Please remove all future schedules before deleting.");


            var hasActiveBookings = await _scheduleRepository.HasActiveBookingsForBusAsync(id);
            if (hasActiveBookings)
                throw new ConflictException(
                    "This bus has upcoming schedules with active bookings. Please refund all booked seats before deleting.");

            bus.IsDeleted = true;
            bus.UpdatedAt = DateTime.UtcNow;

            await _busRepository.UpdateAsync(bus);

            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Invalid user claim.");

            var ip = _httpContextAccessor.HttpContext?
                .Connection.RemoteIpAddress?.ToString();

            await _auditService.LogAsync(
                userId,
                "Soft Delete",
                "Bus",
                bus.BusId.ToString(),
                bus,
                null,
                ip
            );
        }

        public async Task RateBusAsync(int busId, int userId, int rating, string ipAddress)
        {
            var bus = await _busRepository.GetByIdAsync(busId);
            if (bus == null || bus.IsDeleted)
                throw new NotFoundException("Bus not found.");

            // Simple rolling average — recalculate from scratch would need a Ratings table
            // For now: weighted update. To do it properly, add a BusRatings table.
            // Simple approach: update ratingAverage as a weighted moving average
            var currentRating = bus.RatingAverage;
            if (currentRating == 0)
                bus.RatingAverage = rating;
            else
                bus.RatingAverage = Math.Round((currentRating + rating) / 2.0, 1);

            bus.UpdatedAt = DateTime.UtcNow;
            await _busRepository.UpdateAsync(bus);

            await _auditService.LogAsync(userId, "Rate", "Bus", bus.BusId.ToString(),
                new { previousRating = currentRating },
                new { newRating = bus.RatingAverage }, ipAddress);
        }

        private static BusResponse MapToResponse(Bus bus)
        {
            return new BusResponse
            {
                BusId = bus.BusId,
                BusNumber = bus.BusNumber,
                BusType = bus.BusType,
                TotalSeats = bus.TotalSeats,
                OperatorName = bus.OperatorName,
                IsActive = bus.IsActive
            };
        }

        public async Task<(List<BusResponse>, int totalCount)> GetByOperatorAsync(
            string operatorName,
            int pageNumber,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(operatorName))
                throw new BadRequestException("Operator name is required.");

            var (buses, totalCount) =
                await _busRepository.GetByOperatorAsync(operatorName, pageNumber, pageSize);

            var response = buses.Select(b => new BusResponse
            {
                BusId = b.BusId,
                BusType = b.BusType,
                BusNumber = b.BusNumber,
                OperatorName = b.OperatorName,
                TotalSeats = b.TotalSeats,
                IsActive = b.IsActive
            }).ToList();

            return (response, totalCount);
        }
    }
}