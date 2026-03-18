using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{

    public interface IPassengerService
    {
  
        Task<ApiResponse<List<PassengerResponseDto>>> AddPassengersAsync(
            AddPassengerRequestDto dto,
            int userId,
            string ipAddress);

   
        Task<ApiResponse<List<PassengerResponseDto>>> GetBookingPassengersAsync(int bookingId);

     
        Task<ApiResponse<PassengerResponseDto>> UpdatePassengerAsync(
            int passengerId,
            PassengerDetailDto dto,
            int userId,
            string ipAddress);


        Task<ApiResponse<bool>> ValidatePassengersAsync(int bookingId);
    }
}
