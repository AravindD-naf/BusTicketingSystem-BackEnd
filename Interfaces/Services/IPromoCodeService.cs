using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IPromoCodeService
    {
        Task<PromoCodeValidationResponseDto> ValidateAsync(string code, decimal bookingAmount);
        Task IncrementUsageAsync(string code);
    }
}
