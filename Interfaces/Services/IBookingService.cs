using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IBookingService
    {
       
        Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(
            CreateBookingRequestDto dto,
            int userId,
            string ipAddress);

        Task<ApiResponse<List<BookingResponseDto>>>
            GetMyBookingsAsync(int userId);

        Task<ApiResponse<List<BookingResponseDto>>>
            GetAllBookingsAsync();

        Task<ApiResponse<BookingDetailResponseDto>> GetBookingByIdAsync(int bookingId);

 
        Task<ApiResponse<bool>> CancelBookingAsync(
            int bookingId,
            int userId,
            string role,
            string ipAddress);
    }
}
