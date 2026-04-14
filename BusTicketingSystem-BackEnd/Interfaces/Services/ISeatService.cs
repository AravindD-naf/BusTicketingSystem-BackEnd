using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface ISeatService
    {

        Task<ApiResponse<SeatLayoutResponseDto>> GetSeatLayoutAsync(int scheduleId);

        Task<ApiResponse<LockSeatsResponseDto>> LockSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int userId,
            string ipAddress);


        Task<ApiResponse<ReleaseSeatsResponseDto>> ReleaseSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int userId,
            string ipAddress);


        Task<int> CleanupExpiredLocksAsync();


        Task<ApiResponse<bool>> ConfirmBookingSeatsAsync(
            int bookingId,
            int scheduleId,
            List<string> seatNumbers,
            int userId);


        Task<ApiResponse<bool>> ReleaseBookingSeatsAsync(
            int scheduleId,
            List<string> seatNumbers,
            int bookingId);

        /// <summary>
        /// Extends the lock on all seats held by the user for a given schedule.
        /// Called at payment initiation so the lock outlives the payment window.
        /// Returns the number of seats extended.
        /// </summary>
        Task<int> ExtendSeatsLockAsync(int scheduleId, int userId, int extendByMinutes);
    }
}
