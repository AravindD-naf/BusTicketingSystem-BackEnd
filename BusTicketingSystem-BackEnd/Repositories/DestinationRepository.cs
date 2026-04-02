using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{


    public class DestinationRepository : Repository<Destination>, IDestinationRepository
    {
        public DestinationRepository(ApplicationDbContext context) : base(context) { }

        public new async Task<Destination?> GetByIdAsync(int id)
        {
            return await _context.Destinations
                .Where(d => d.DestinationId == id && !d.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Destination>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Destinations
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.DestinationName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Destination>> GetByCityAsync(string cityName)
        {
            var city = cityName.Trim().ToLower();
            return await _context.Destinations
                .Where(d => !d.IsDeleted && d.IsActive &&
                            d.DestinationName.ToLower().StartsWith(city))
                .OrderBy(d => d.DestinationName)
                .ToListAsync();
        }

        public async Task<(List<Destination> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Destinations
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.DestinationId);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task CreateAsync(Destination destination)
        {
            _context.Destinations.Add(destination);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Destination destination)
        {
            _context.Destinations.Update(destination);
            await _context.SaveChangesAsync();
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

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var normalizedName = name.Trim().ToLower();
            return await _context.Destinations
                .Where(d => !d.IsDeleted &&
                       d.DestinationName.ToLower() == normalizedName &&
                       (excludeId == null || d.DestinationId != excludeId))
                .AnyAsync();
        }
    }
}
