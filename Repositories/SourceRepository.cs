using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{

    public class SourceRepository : Repository<Source>, ISourceRepository
    {
        public SourceRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Source> GetByIdAsync(int id)
        {
            return await _context.Sources
                .Where(s => s.SourceId == id && !s.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<Source> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Sources
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SourceId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<Source>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Sources
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SourceName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Source>> GetByCityAsync(string cityName)
        {
            var city = cityName.Trim().ToLower();
            return await _context.Sources
                .Where(s => !s.IsDeleted && s.IsActive &&
                            s.SourceName.ToLower().StartsWith(city))
                .OrderBy(s => s.SourceName)
                .ToListAsync();
        }

        public async Task CreateAsync(Source source)
        {
            _context.Sources.Add(source);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string sourcename)
        {
            return await _context.Sources.AnyAsync(s => s.SourceName.ToLower() == sourcename.ToLower() && !s.IsDeleted);
        }

        public async Task UpdateAsync(Source source)
        {
            _context.Sources.Update(source);
            await _context.SaveChangesAsync();
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
