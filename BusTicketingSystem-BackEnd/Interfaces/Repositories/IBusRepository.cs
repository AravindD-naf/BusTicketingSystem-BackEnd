using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IBusRepository : IRepository<Bus>
    {
        Task<Bus> CreateAsync(Bus bus);
        Task<Bus?> GetByBusNumberAsync(string busNumber);
        Task<List<Bus>> GetAllAsync(int pageNumber, int pageSize);
        Task<(List<Bus> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<int> CountAsync();
        Task UpdateAsync(Bus bus);
        Task<(List<Bus>, int totalCount)> GetByOperatorAsync(string operatorName, int pageNumber, int pageSize);
    }
}