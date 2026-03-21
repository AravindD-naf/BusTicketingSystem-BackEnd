using BusTicketingSystem.DTOs.Requests;
using BusTicketingSystem.DTOs.Responses;

namespace BusTicketingSystem.Interfaces.Services
{
    public interface IBusService
    {
        Task<BusResponse> CreateBusAsync(CreateBusRequest request,
            int userId,
            string? ipAddress);
        Task<BusResponse> GetBusByIdAsync(int id);
        Task<(List<BusResponse> items, int totalCount)> GetAllBusesAsync(int pageNumber, int pageSize);
        Task UpdateBusAsync(int id,
            UpdateBusRequest request,
            int userId,
            string? ipAddress);
        Task DeleteBusAsync(int id);
        Task RateBusAsync(int busId, int userId, int rating, string ipAddress);
        Task<(List<BusResponse>, int totalCount)> GetByOperatorAsync(
            string operatorName,
            int pageNumber,
            int pageSize);
    }
}