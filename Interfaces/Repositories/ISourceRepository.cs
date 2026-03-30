using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISourceRepository : IRepository<Source>
    {
        new Task<Source?> GetByIdAsync(int id);
        Task<List<Source>> GetAllAsync(int pageNumber, int pageSize);
        Task<List<Source>> GetByCityAsync(string cityName);
        Task<(List<Source> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task CreateAsync(Source source);
        Task UpdateAsync(Source source);
        Task<bool> ExistsByNameAsync(string sourceName);
        Task DeleteAsync(int id);
    }
}