using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IPromoCodeService
    {
        Task<PromoCodeValidationResponseDto> ValidateAsync(string code, decimal bookingAmount);
        Task IncrementUsageAsync(string code);
        Task<List<PromoCodeResponseDto>> GetAllAsync();
        Task<List<PromoCodeResponseDto>> GetActiveAsync();
        Task<PromoCodeResponseDto> CreateAsync(CreatePromoCodeRequest request);
        Task<PromoCodeResponseDto> UpdateAsync(int id, UpdatePromoCodeRequest request);
        Task DeleteAsync(int id);
        Task<PromoCodeResponseDto?> ToggleActiveAsync(int id);
    }
}
