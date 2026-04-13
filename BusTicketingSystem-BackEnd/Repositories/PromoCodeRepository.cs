using BusTicketingSystem.Data;
using BusTicketingSystem.Interfaces.Repositories;
using BusTicketingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketingSystem.Repositories
{
    public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
    {
        public PromoCodeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<PromoCode?> GetByCodeAsync(string code) =>
            await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == code.ToUpper().Trim());

        public new async Task<PromoCode?> GetByIdAsync(int id) =>
            await _context.PromoCodes.FindAsync(id);

        public new async Task<List<PromoCode>> GetAllAsync() =>
            await _context.PromoCodes
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

        public async Task<List<PromoCode>> GetActiveAsync() =>
            await _context.PromoCodes
                .Where(p => p.IsActive &&
                            p.ValidFrom <= DateTime.UtcNow &&
                            p.ValidUntil >= DateTime.UtcNow)
                .OrderBy(p => p.ValidUntil)
                .ToListAsync();

        public async Task<bool> CodeExistsAsync(string code, int? excludeId = null) =>
            await _context.PromoCodes
                .AnyAsync(p => p.Code == code.ToUpper().Trim() &&
                               (excludeId == null || p.PromoCodeId != excludeId));

        public new async Task AddAsync(PromoCode promoCode) =>
            await _context.PromoCodes.AddAsync(promoCode);

        public async Task UpdateAsync(PromoCode promoCode)
        {
            _context.PromoCodes.Update(promoCode);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(PromoCode promoCode)
        {
            _context.PromoCodes.Remove(promoCode);
            await Task.CompletedTask;
        }

        public new async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
