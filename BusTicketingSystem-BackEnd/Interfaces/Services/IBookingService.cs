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

        Task<int> CleanupExpiredBookingsAsync();

        Task<(List<BookingResponseDto> items, int totalCount)> GetAllBookingsAsync(int pageNumber, int pageSize);

        Task<ApiResponse<BookingDetailResponseDto>> GetBookingByIdAsync(int bookingId);

        Task<ApiResponse<bool>> CancelBookingAsync(
            int bookingId,
            int userId,
            string role,
            string ipAddress);

        Task<ApiResponse<bool>> RateBookingAsync(
            int bookingId,
            int userId,
            int rating);
    }
}
