using BusTicketingSystem.Common.Responses;
using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Helpers;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IRouteService
    {
        Task<ApiResponse<RouteResponseDto>> CreateRouteAsync(RouteCreateRequestDto request, int userId, string ipAddress);
        Task<ApiResponse<RouteResponseDto>> UpdateRouteAsync(int id, RouteUpdateRequestDto request, int userId, string ipAddress);
        Task<ApiResponse<string>> DeleteRouteAsync(int id, int userId, string ipAddress);
        Task<ApiResponse<PagedResponse<RouteResponseDto>>> GetAllRoutesAsync(int pageNumber, int pageSize);
        Task<ApiResponse<RouteResponseDto>> GetRouteByIdAsync(int id);

        Task<ApiResponse<PagedResponse<RouteResponseDto>>> SearchRoutesAsync(
    string? source,
    string? destination,
    int pageNumber,
    int pageSize);

        Task<ApiResponse<PagedResponse<RouteResponseDto>>>
    GetRoutesBySourceAsync(string source, int pageNumber, int pageSize);

        Task<ApiResponse<PagedResponse<RouteResponseDto>>>
            GetRoutesByDestinationAsync(string destination, int pageNumber, int pageSize);

    }
}