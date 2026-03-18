using BusTicketingSystem.Common.Responses;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Helpers;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Interfaces.Services;
using BusTicketingSystem.Models;
using System.Text.Encodings.Web;
using System.Text.Json;
namespace BusTicketingSystem.Services
{
    public class RouteService : IRouteService
    {
        private readonly IRouteRepository _routeRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly JsonSerializerOptions _jsonOptions;

        public RouteService(IRouteRepository routeRepository,
                            IAuditRepository auditRepository)
        {
            _routeRepository = routeRepository;
            _auditRepository = auditRepository;
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
        }

        public async Task<ApiResponse<RouteResponseDto>> CreateRouteAsync(RouteCreateRequestDto request, int userId, string ipAddress)
        {
            var existing = await _routeRepository
                .GetBySourceDestinationAsync(request.Source.Trim(), request.Destination.Trim());

            if (existing != null && !existing.IsDeleted)
                throw new ConflictException($"Route from {request.Source} to {request.Destination} already exists");

            var route = new Models.Route
            {
                Source = request.Source.Trim(),
                Destination = request.Destination.Trim(),
                Distance = request.Distance,
                EstimatedTravelTimeMinutes = request.EstimatedTravelTimeMinutes,
                BaseFare = request.BaseFare,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _routeRepository.AddAsync(route);
            await _routeRepository.SaveChangesAsync();

            await LogAuditAsync("Create", route, null, route, userId, ipAddress);

            return ApiResponse<RouteResponseDto>.SuccessResponse(MapToDto(route));
        }

        public async Task<ApiResponse<RouteResponseDto>> UpdateRouteAsync(int id, RouteUpdateRequestDto request, int userId, string ipAddress)
        {
            var route = await _routeRepository.GetByIdAsync(id)
                ?? throw new ResourceNotFoundException("Route", id.ToString());

            var oldValues = JsonSerializer.Serialize(route);

            route.Source = request.Source.Trim();
            route.Destination = request.Destination.Trim();
            route.Distance = request.Distance;
            route.EstimatedTravelTimeMinutes = request.EstimatedTravelTimeMinutes;
            route.BaseFare = request.BaseFare;
        
            if (request.IsActive.HasValue)
            {
                route.IsActive = request.IsActive.Value;
            }
            
            route.UpdatedAt = DateTime.UtcNow;

            _routeRepository.Update(route);
            await _routeRepository.SaveChangesAsync();

            await LogAuditAsync("Update", route, oldValues, route, userId, ipAddress);

            return ApiResponse<RouteResponseDto>.SuccessResponse(MapToDto(route));
        }

        public async Task<ApiResponse<string>> DeleteRouteAsync(int id, int userId, string ipAddress)
        {
            var route = await _routeRepository.GetByIdAsync(id)
                ?? throw new Exception("Route not found.");

            var oldValues = JsonSerializer.Serialize(route);

            route.IsDeleted = true;
            route.UpdatedAt = DateTime.UtcNow;

            _routeRepository.Update(route);
            await _routeRepository.SaveChangesAsync();

            await LogAuditAsync("Delete", route, oldValues, null, userId, ipAddress);

            return ApiResponse<string>.SuccessResponse("Route deleted successfully.");
        }

        public async Task<ApiResponse<PagedResponse<RouteResponseDto>>> GetAllRoutesAsync(int pageNumber, int pageSize)
        {
            var (routes, totalCount) = await _routeRepository
                .GetPagedAsync(pageNumber, pageSize);

            var mapped = routes.Select(MapToDto).ToList();

            var paged = new PagedResponse<RouteResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<RouteResponseDto>>.SuccessResponse(paged);
        }

        public async Task<ApiResponse<RouteResponseDto>> GetRouteByIdAsync(int id)
        {
            var route = await _routeRepository.GetByIdAsync(id)
                ?? throw new Exception("Route not found.");

            return ApiResponse<RouteResponseDto>.SuccessResponse(MapToDto(route));
        }

        private RouteResponseDto MapToDto(Models.Route route)
        {
            return new RouteResponseDto
            {
                RouteId = route.RouteId,
                Source = route.Source,
                Destination = route.Destination,
                Distance = route.Distance,
                EstimatedTravelTimeMinutes = route.EstimatedTravelTimeMinutes,
                BaseFare = route.BaseFare,
                IsActive = route.IsActive,
                CreatedAt = route.CreatedAt,
                UpdatedAt = route.UpdatedAt
            };
        }

        private async Task LogAuditAsync(string action, Models.Route route, object? oldValues, object? newValues, int userId, string ipAddress)
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = "Route",
                EntityId = route.RouteId.ToString(),
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, _jsonOptions) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues, _jsonOptions) : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(audit);
            await _auditRepository.SaveChangesAsync();
        }

        public async Task<ApiResponse<PagedResponse<RouteResponseDto>>>
    GetRoutesBySourceAsync(string source, int pageNumber, int pageSize)
        {
            var (routes, totalCount) = await _routeRepository
                .GetBySourceAsync(source.Trim(), pageNumber, pageSize);

            var mapped = routes.Select(MapToDto).ToList();

            var paged = new PagedResponse<RouteResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<RouteResponseDto>>.SuccessResponse(paged);
        }

        public async Task<ApiResponse<PagedResponse<RouteResponseDto>>>
            GetRoutesByDestinationAsync(string destination, int pageNumber, int pageSize)
        {
            var (routes, totalCount) = await _routeRepository
                .GetByDestinationAsync(destination.Trim(), pageNumber, pageSize);

            var mapped = routes.Select(MapToDto).ToList();

            var paged = new PagedResponse<RouteResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<RouteResponseDto>>.SuccessResponse(paged);
        }

        public async Task<ApiResponse<PagedResponse<RouteResponseDto>>> SearchRoutesAsync(
    string? source,
    string? destination,
    int pageNumber,
    int pageSize)
        {
            if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(destination))
                throw new Exception("At least one search parameter must be provided.");

            var (routes, totalCount) = await _routeRepository
                .SearchAsync(source, destination, pageNumber, pageSize);

            var mapped = routes.Select(MapToDto).ToList();

            var paged = new PagedResponse<RouteResponseDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResponse<RouteResponseDto>>.SuccessResponse(paged);
        }
    }
}