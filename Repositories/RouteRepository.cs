using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class RouteRepository : IRouteRepository
    {
        private readonly ApplicationDbContext _context;

        public RouteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Models.Route?> GetByIdAsync(int id)
        {
            return await _context.Routes
                .FirstOrDefaultAsync(r => r.RouteId == id);
        }

        public async Task<Models.Route?> GetBySourceDestinationAsync(string source, string destination)
        {
            return await _context.Routes
                .IgnoreQueryFilters() // needed to check duplicates including soft-deleted
                .FirstOrDefaultAsync(r =>
                    r.Source.ToLower() == source.ToLower() &&
                    r.Destination.ToLower() == destination.ToLower());
        }

        public async Task<(IEnumerable<Models.Route> Routes, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Routes.AsQueryable();

            var totalCount = await query.CountAsync();

            var routes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (routes, totalCount);
        }

        public async Task AddAsync(Models.Route route)
        {
            await _context.Routes.AddAsync(route);
        }

        public void Update(Models.Route route)
        {
            _context.Routes.Update(route);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<Models.Route> Routes, int TotalCount)>
            GetBySourceAsync(string source, int pageNumber, int pageSize)
        {
            var query = _context.Routes
                .Where(r => r.Source.ToLower() == source.ToLower());

            var totalCount = await query.CountAsync();

            var routes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (routes, totalCount);
        }

        public async Task<(IEnumerable<Models.Route> Routes, int TotalCount)>
            GetByDestinationAsync(string destination, int pageNumber, int pageSize)
        {
            var query = _context.Routes
                .Where(r => r.Destination.ToLower() == destination.ToLower());

            var totalCount = await query.CountAsync();

            var routes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (routes, totalCount);
        }

        public async Task<(IEnumerable<Models.Route> Routes, int TotalCount)> SearchAsync(
            string? source,
            string? destination,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Routes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(source))
            {
                var normalizedSource = source.Trim().ToLower();
                query = query.Where(r => r.Source.ToLower() == normalizedSource);
            }

            if (!string.IsNullOrWhiteSpace(destination))
            {
                var normalizedDestination = destination.Trim().ToLower();
                query = query.Where(r => r.Destination.ToLower() == normalizedDestination);
            }

            var totalCount = await query.CountAsync();

            var routes = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (routes, totalCount);
        }
    }
}