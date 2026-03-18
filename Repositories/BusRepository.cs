using BusTicketingSystem.Data;
using BusTicketingSystem.Exceptions;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class BusRepository : IBusRepository
    {
        private readonly ApplicationDbContext _context;

        public BusRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Bus> CreateAsync(Bus bus)
        {
            //await _context.Buses.AddAsync(bus);
            //await _context.SaveChangesAsync();
            //return bus;

            try
            {
                await _context.Buses.AddAsync(bus);
                await _context.SaveChangesAsync();
                return bus;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("IX_Buses_BusNumber") == true)
                    throw new BadRequestException("Bus number already exists.");

                throw;
            }
        }

        public async Task<Bus?> GetByIdAsync(int id)
        {
            return await _context.Buses
                .FirstOrDefaultAsync(b => b.BusId == id && !b.IsDeleted);
        }

        public async Task<Bus?> GetByBusNumberAsync(string busNumber)
        {
            return await _context.Buses
                .FirstOrDefaultAsync(b => b.BusNumber == busNumber && !b.IsDeleted);
        }

        public async Task<List<Bus>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Buses
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Buses.CountAsync(b => !b.IsDeleted);
        }

        public async Task UpdateAsync(Bus bus)
        {
            _context.Buses.Update(bus);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<Bus>, int totalCount)> GetByOperatorAsync(
            string operatorName,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Buses
                .Where(b => !b.IsDeleted &&
                            EF.Functions.Like(b.OperatorName, $"%{operatorName}%"));

            var totalCount = await query.CountAsync();

            var buses = await query
                .OrderBy(b => b.BusId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (buses, totalCount);
        }


    }
}