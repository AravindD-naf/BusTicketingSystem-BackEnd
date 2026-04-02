using BusTicketingSystem.Data;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetQueryable();
        Task<T?> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        Task SaveChangesAsync();
    }
}