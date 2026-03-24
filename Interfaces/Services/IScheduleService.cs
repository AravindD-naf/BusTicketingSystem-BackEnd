using BusTicketingSystem.Common.Responses;
using BusTicketingSystem.DTOs;
using BusTicketingSystem.DTOs.Requests;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IScheduleService
    {
        Task<ApiResponse<ScheduleResponseDto>> CreateAsync(
            ScheduleRequestDto dto,
            int userId,
            string ipAddress);

        Task<ApiResponse<PagedResponse<ScheduleResponseDto>>>
            GetAllAsync(int pageNumber, int pageSize, string? keyword = null);

        Task<ApiResponse<ScheduleResponseDto>> GetByIdAsync(int id);

        Task<ApiResponse<ScheduleResponseDto>> UpdateAsync(
            int id,
            ScheduleRequestDto dto,
            int userId,
            string ipAddress);

        Task<ApiResponse<bool>> DeleteAsync(
            int id,
            int userId,
            string ipAddress);

        Task<ApiResponse<List<ScheduleResponseDto>>>
            GetByFromCityAsync(string fromCity);

        Task<ApiResponse<List<ScheduleResponseDto>>>
            GetByToCityAsync(string toCity);

        Task<ApiResponse<PagedResponse<ScheduleResponseDto>>> SearchSchedulesAsync(ScheduleSearchRequest request);


    }
}