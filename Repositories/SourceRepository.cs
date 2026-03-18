using BusTicketingSystem.Data;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public interface ISourceRepository
    {
        Task<Source> GetByIdAsync(int id);
        Task<List<Source>> GetAllAsync(int pageNumber, int pageSize);
        Task CreateAsync(Source source);
        Task<bool> ExistsByNameAsync(string sourcename);
        Task UpdateAsync(Source source);
        Task DeleteAsync(int id);
    }

    public class SourceRepository : ISourceRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SourceRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Source> GetByIdAsync(int id)
        {
            return await _dbContext.Sources
                .Where(s => s.SourceId == id && !s.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Source>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _dbContext.Sources
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SourceName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task CreateAsync(Source source)
        {
            _dbContext.Sources.Add(source);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string sourcename)
        {
            return await _dbContext.Sources.AnyAsync(s => s.SourceName.ToLower() == sourcename.ToLower() && !s.IsDeleted);
        }

        public async Task UpdateAsync(Source source)
        {
            _dbContext.Sources.Update(source);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var source = await GetByIdAsync(id);
            if (source != null)
            {
                source.IsDeleted = true;
                source.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(source);
            }
        }
    }
}
