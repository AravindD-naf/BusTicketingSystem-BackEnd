using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;
using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Services
{
 
    public interface IPaymentService
    {
      
        Task<ApiResponse<PaymentResponseDto>> InitiatePaymentAsync(
            int bookingId,
            decimal amount,
            string paymentMethod,
            int userId,
            string ipAddress
            );

  
        Task<ApiResponse<PaymentResponseDto>> GetPaymentAsync(int paymentId);

        Task<ApiResponse<PaymentResponseDto>> ConfirmPaymentAsync(
            ConfirmPaymentRequestDto dto,
            int userId,
            string ipAddress);

        Task<ApiResponse<RefundResponseDto>> InitiateRefundAsync(
            int bookingId,
            int userId,
            string ipAddress);

  
        Task<ApiResponse<RefundResponseDto>> GetRefundAsync(int refundId);

   
        Task<ApiResponse<RefundResponseDto>> ConfirmRefundAsync(
            ConfirmRefundRequestDto dto,
            int userId,
            string ipAddress);

 
        Task<int> ExpireOldPaymentsAsync();


        Task<(decimal refundAmount, int refundPercentage, decimal cancellationFee)> CalculateRefundAsync(
            int bookingId);
    }
}
