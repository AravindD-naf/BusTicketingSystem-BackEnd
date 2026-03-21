using BusTicketingSystem.Data;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public interface IDestinationRepository
    {
        Task<Destination> GetByIdAsync(int id);
        Task<List<Destination>> GetAllAsync(int pageNumber, int pageSize);
        Task<List<Destination>> GetByCityAsync(string cityName);
        Task CreateAsync(Destination destination);
        Task UpdateAsync(Destination destination);
        Task DeleteAsync(int id);
    }

    public class DestinationRepository : IDestinationRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public DestinationRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Destination> GetByIdAsync(int id)
        {
            return await _dbContext.Destinations
                .Where(d => d.DestinationId == id && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Destination>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _dbContext.Destinations
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.DestinationName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Destination>> GetByCityAsync(string cityName)
        {
            var city = cityName.Trim().ToLower();
            return await _dbContext.Destinations
                .Where(d => !d.IsDeleted && d.IsActive &&
                            d.DestinationName.ToLower().StartsWith(city))
                .OrderBy(d => d.DestinationName)
                .ToListAsync();
        }

        public async Task<(List<Source> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _dbContext.Sources.Where(s => !s.IsDeleted).OrderBy(s => s.SourceId);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task CreateAsync(Destination destination)
        {
            _dbContext.Destinations.Add(destination);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Destination destination)
        {
            _dbContext.Destinations.Update(destination);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var destination = await GetByIdAsync(id);
            if (destination != null)
            {
                destination.IsDeleted = true;
                destination.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(destination);
            }
        }
    }
}
