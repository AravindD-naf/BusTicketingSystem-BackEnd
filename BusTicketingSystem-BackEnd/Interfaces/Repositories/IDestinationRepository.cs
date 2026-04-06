using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IDestinationRepository : IRepository<Destination>
    {
        new Task<Destination?> GetByIdAsync(int id);
        Task<List<Destination>> GetAllAsync(int pageNumber, int pageSize);
        Task<List<Destination>> GetByCityAsync(string cityName);
        Task<(List<Destination> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task CreateAsync(Destination destination);
        Task UpdateAsync(Destination destination);
        Task DeleteAsync(int id);
    }
}