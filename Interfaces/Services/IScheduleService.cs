using BusTicketingSystem.Common.Responses;
using BusTicketingSystem.DTOs;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IScheduleService
    {
        Task<ApiResponse<ScheduleResponseDto>> CreateAsync(
            ScheduleRequestDto dto,
            int userId,
            string ipAddress);

        Task<ApiResponse<PagedResponse<ScheduleResponseDto>>>
            GetAllAsync(int pageNumber, int pageSize);

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

        // 3️⃣ Get by From + To + Travel Date (Most Important for Users)
        Task<ApiResponse<List<ScheduleResponseDto>>>
            SearchSchedulesAsync(
                string fromCity,
                string toCity,
                DateTime travelDate);
    }
}