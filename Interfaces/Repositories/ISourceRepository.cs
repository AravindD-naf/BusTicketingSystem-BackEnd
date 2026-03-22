using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface ISourceRepository : IRepository<Source>
    {
        Task<Source> GetByIdAsync(int id);
        Task<List<Source>> GetAllAsync(int pageNumber, int pageSize);
        Task<List<Source>> GetByCityAsync(string cityName);
        Task<(List<Source> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task CreateAsync(Source source);
        Task<bool> ExistsByNameAsync(string sourceName);
        Task UpdateAsync(Source source);
        Task DeleteAsync(int id);
    }
}