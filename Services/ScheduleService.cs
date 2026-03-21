using BusTicketingSystem.Common.Responses;
using BusTicketingSystem.Data;
using BusTicketingSystem.DTOs;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BusTicketingSystem.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IRouteRepository _routeRepository;
        private readonly IBusRepository _busRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ApplicationDbContext _dbContext;
        private readonly JsonSerializerOptions _jsonOptions;

        public ScheduleService(
            IScheduleRepository scheduleRepository,
            IRouteRepository routeRepository,
            IBusRepository busRepository,
            IAuditRepository auditRepository,
            ApplicationDbContext dbContext)
        {
            _scheduleRepository = scheduleRepository;
            _routeRepository = routeRepository;
            _busRepository = busRepository;
            _auditRepository = auditRepository;
            _dbContext = dbContext;
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
        }

        public async Task<ApiResponse<ScheduleResponseDto>> CreateAsync(
            ScheduleRequestDto dto,
            int userId,
            string ipAddress)
        {
            var depTime = dto.DepartureTimeSpan;
            var arrTime = dto.ArrivalTimeSpan;

            if (arrTime == depTime)
                throw ValidationException.ForField("arrivalTime", "Arrival time cannot equal departure time");

            if (dto.TravelDate.Date < DateTime.UtcNow.Date)
                throw ValidationException.ForField("travelDate", "Travel date cannot be in the past");

            var route = await _routeRepository.GetByIdAsync(dto.RouteId);
            if (route == null || route.IsDeleted || !route.IsActive)
                throw new ResourceNotFoundException("Route", dto.RouteId.ToString());

            var bus = await _busRepository.GetByIdAsync(dto.BusId);
            if (bus == null || bus.IsDeleted || !bus.IsActive)
                throw new ResourceNotFoundException("Bus", dto.BusId.ToString());

            // Check 1: exact same time
            var exactExists = await _scheduleRepository
                .ExistsAsync(dto.BusId, dto.TravelDate, depTime);
            if (exactExists)
                throw new ConflictException(
                    $"Bus is already scheduled on {dto.TravelDate:dd MMM yyyy} at {depTime:hh\\:mm}. " +
                    "Please choose a different departure time.");

            // Check 2: journey overlap (bus can't be in two places)
            var arrWrappedCheck = arrTime.TotalMinutes >= 1440
                ? TimeSpan.FromMinutes(arrTime.TotalMinutes % 1440)
                : arrTime;
            bool isOvernightCheck = arrTime.TotalMinutes >= 1440;

            var hasOverlap = await _scheduleRepository.HasOverlappingScheduleAsync(
                dto.BusId, dto.TravelDate, depTime, arrWrappedCheck, isOvernightCheck);
            if (hasOverlap)
                throw new ConflictException(
                    $"Bus is already on a journey that overlaps with the requested time on {dto.TravelDate:dd MMM yyyy}. " +
                    "Please choose a time after the existing journey completes.");

            // Wrap arrival to 0-23h range for DB storage (time column max 23:59:59)
            var arrWrapped = arrTime.TotalMinutes >= 1440
                ? TimeSpan.FromMinutes(arrTime.TotalMinutes % 1440)
                : arrTime;
            bool isOvernight = arrTime.TotalMinutes >= 1440;

            var schedule = new Schedule
            {
                RouteId = dto.RouteId,
                BusId = dto.BusId,
                TravelDate = dto.TravelDate.Date,
                DepartureTime = depTime,
                ArrivalTime = arrWrapped,
                IsOvernightArrival = isOvernight,
                TotalSeats = bus.TotalSeats,
                AvailableSeats = bus.TotalSeats,
                CreatedAt = DateTime.UtcNow
            };

            await _scheduleRepository.AddAsync(schedule);
            await _scheduleRepository.SaveChangesAsync();

            // Generate seats directly in C# — no stored procedure dependency
            try
            {
                var seats = new List<Seat>();
                int col = 1;
                char row = 'A';

                for (int i = 1; i <= schedule.TotalSeats; i++)
                {
                    seats.Add(new Seat
                    {
                        ScheduleId = schedule.ScheduleId,
                        SeatNumber = $"{row}{col}",
                        SeatStatus = "Available",
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });

                    col++;
                    if (col > 4)
                    {
                        col = 1;
                        row = (char)(row + 1);
                    }
                }

                await _dbContext.Seats.AddRangeAsync(seats);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating seats for schedule: " + ex.Message);
            }

            // Activate the bus when it's scheduled to a route
            var busToUpdate = await _busRepository.GetByIdAsync(dto.BusId);
            if (busToUpdate != null && !busToUpdate.IsActive)
            {
                busToUpdate.IsActive = true;
                busToUpdate.UpdatedAt = DateTime.UtcNow;
                await _busRepository.UpdateAsync(busToUpdate);
            }

            await _auditRepository.LogAuditAsync(
                "CREATE",
                "Schedule",
                schedule.ScheduleId.ToString(),
                null,
                schedule,
                userId,
                ipAddress);

            return ApiResponse<ScheduleResponseDto>
                .SuccessResponse(MapToDto(schedule));
        }

        public async Task<ApiResponse<PagedResponse<ScheduleResponseDto>>>
            GetAllAsync(int pageNumber, int pageSize)
        {
            var (schedules, totalCount) =
                await _scheduleRepository.GetPagedAsync(pageNumber, pageSize);

            var mapped = schedules.Select(MapToDto).ToList();

            var paged = new PagedResponse<ScheduleResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<ScheduleResponseDto>>
                .SuccessResponse(paged);
        }

        public async Task<ApiResponse<ScheduleResponseDto>> GetByIdAsync(int id)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
                throw new Exception("Schedule not found.");

            return ApiResponse<ScheduleResponseDto>
                .SuccessResponse(MapToDto(schedule));
        }


        public async Task<ApiResponse<ScheduleResponseDto>> UpdateAsync(
            int id,
            ScheduleRequestDto dto,
            int userId,
            string ipAddress)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null || schedule.IsDeleted)
                throw new ResourceNotFoundException("Schedule", id.ToString());

            if (dto.TravelDate.Date < DateTime.UtcNow.Date)
                throw new ValidationException("Travel date cannot be in the past.");

            var depTime = dto.DepartureTimeSpan;
            var arrTime = dto.ArrivalTimeSpan;

            if (arrTime.TotalMinutes == 0 && depTime.TotalMinutes == 0)
                throw new ValidationException("Invalid departure or arrival time.");

            if (arrTime == depTime && arrTime.TotalMinutes < 1440)
                throw new ValidationException("Arrival time cannot equal departure time.");

            if (dto.TravelDate.Date == DateTime.UtcNow.Date &&
                dto.TravelDate.Date.Add(depTime) <= DateTime.UtcNow)
                throw new ValidationException("Departure time must be in the future.");

            var bus = await _busRepository.GetByIdAsync(dto.BusId);
            if (bus == null || bus.IsDeleted || !bus.IsActive)
                throw new ResourceNotFoundException("Bus", dto.BusId.ToString());

            var route = await _routeRepository.GetByIdAsync(dto.RouteId);
            if (route == null || route.IsDeleted || !route.IsActive)
                throw new ResourceNotFoundException("Route", dto.RouteId.ToString());

            // Wrap arrival
            var arrWrapped = arrTime.TotalMinutes >= 1440
                ? TimeSpan.FromMinutes(arrTime.TotalMinutes % 1440)
                : arrTime;
            bool isOvernight = arrTime.TotalMinutes >= 1440;

            // Conflict check — exclude current schedule
            var exactExists = await _scheduleRepository
                .ExistsAsync(dto.BusId, dto.TravelDate, depTime);
            if (exactExists)
            {
                // Only a conflict if it's a DIFFERENT schedule
                var conflicting = await _dbContext.Schedules
                    .AnyAsync(s =>
                        s.BusId == dto.BusId &&
                        s.TravelDate.Date == dto.TravelDate.Date &&
                        s.DepartureTime == depTime &&
                        s.ScheduleId != id &&
                        !s.IsDeleted);

                if (conflicting)
                    throw new ConflictException(
                        $"Bus is already scheduled on {dto.TravelDate:dd MMM yyyy} at {depTime:hh\\:mm}.");
            }

            var hasOverlap = await _scheduleRepository.HasOverlappingScheduleAsync(
                dto.BusId, dto.TravelDate, depTime, arrWrapped, isOvernight,
                excludeScheduleId: id);
            if (hasOverlap)
                throw new ConflictException(
                    $"Bus has an overlapping journey on {dto.TravelDate:dd MMM yyyy}. " +
                    "Please choose a time after the existing journey completes.");

            // Capture old values safely (no navigation props)
            var oldValues = new
            {
                schedule.RouteId,
                schedule.BusId,
                TravelDate = schedule.TravelDate.ToString("yyyy-MM-dd"),
                DepartureTime = schedule.DepartureTime.ToString(),
                ArrivalTime = schedule.ArrivalTime.ToString(),
                schedule.IsOvernightArrival
            };

            bool busChanged = schedule.BusId != dto.BusId;
            if (busChanged)
            {
                int bookedSeats = schedule.TotalSeats - schedule.AvailableSeats;
                if (bookedSeats > bus.TotalSeats)
                    throw new ConflictException(
                        "Cannot change bus. New bus has fewer seats than already booked seats.");
                schedule.TotalSeats = bus.TotalSeats;
                schedule.AvailableSeats = bus.TotalSeats - bookedSeats;
            }

            schedule.BusId = dto.BusId;
            schedule.RouteId = dto.RouteId;
            schedule.TravelDate = dto.TravelDate.Date;
            schedule.DepartureTime = depTime;
            schedule.ArrivalTime = arrWrapped;
            schedule.IsOvernightArrival = isOvernight;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _scheduleRepository.UpdateAsync(schedule);
            await _scheduleRepository.SaveChangesAsync();

            var newValues = new
            {
                schedule.RouteId,
                schedule.BusId,
                TravelDate = schedule.TravelDate.ToString("yyyy-MM-dd"),
                DepartureTime = schedule.DepartureTime.ToString(),
                ArrivalTime = schedule.ArrivalTime.ToString(),
                schedule.IsOvernightArrival
            };

            await _auditRepository.LogAuditAsync(
                "UPDATE", "Schedule", schedule.ScheduleId.ToString(),
                oldValues, newValues, userId, ipAddress);

            // Reload with navigation props for response
            var updated = await _scheduleRepository.GetByIdAsync(id);
            return ApiResponse<ScheduleResponseDto>.SuccessResponse(MapToDto(updated!));
        }


        public async Task<ApiResponse<bool>> DeleteAsync(
            int id,
            int userId,
            string ipAddress)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
                throw new Exception("Schedule not found.");

            schedule.IsDeleted = true;
            schedule.IsActive = false;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _scheduleRepository.UpdateAsync(schedule);
            await _scheduleRepository.SaveChangesAsync();

            await _auditRepository.LogAuditAsync(
                "DELETE",
                "Schedule",
                schedule.ScheduleId.ToString(),
                schedule,
                null,
                userId,
                ipAddress);

            return ApiResponse<bool>.SuccessResponse(true);
        }


        public async Task<ApiResponse<List<ScheduleResponseDto>>>
            GetByFromCityAsync(string fromCity)
        {
            var schedules = await _scheduleRepository
                .GetByFromCityAsync(fromCity);

            return ApiResponse<List<ScheduleResponseDto>>
                .SuccessResponse(schedules.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<ScheduleResponseDto>>>
            GetByToCityAsync(string toCity)
        {
            var schedules = await _scheduleRepository
                .GetByToCityAsync(toCity);

            return ApiResponse<List<ScheduleResponseDto>>
                .SuccessResponse(schedules.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<ScheduleResponseDto>>>
            SearchSchedulesAsync(
                string fromCity,
                string toCity,
                DateTime travelDate)
        {
            var schedules = await _scheduleRepository
                .SearchSchedulesAsync(fromCity, toCity, travelDate.Date);

            return ApiResponse<List<ScheduleResponseDto>>
                .SuccessResponse(schedules.Select(MapToDto).ToList());
        }

        private ScheduleResponseDto MapToDto(Schedule s)
        {
            return new ScheduleResponseDto
            {
                ScheduleId = s.ScheduleId,
                RouteId = s.RouteId,
                Source = s.Route?.Source,
                Destination = s.Route?.Destination,
                BaseFare = s.Route?.BaseFare ?? 0,
                BusId = s.BusId,
                BusNumber = s.Bus?.BusNumber,
                OperatorName = s.Bus?.OperatorName,
                BusType = s.Bus?.BusType,
                Rating = s.Bus?.RatingAverage ?? 0,
                TravelDate = s.TravelDate,
                DepartureTime = s.DepartureTime,
                ArrivalTime = s.ArrivalTime,
                IsOvernightArrival = s.IsOvernightArrival,
                TotalSeats = s.TotalSeats,
                AvailableSeats = s.AvailableSeats,
                IsActive = s.IsActive
            };
        }
    }
}