using BusTicketingSystem.Models;

namespace BusTicketingSystem.Interfaces.Repositories
{
    public interface IPromoCodeRepository : IRepository<PromoCode>
    {
        Task<PromoCode?> GetByCodeAsync(string code);
        Task<PromoCode?> GetByIdAsync(int id);
        Task<List<PromoCode>> GetAllAsync();
        Task<List<PromoCode>> GetActiveAsync();
        Task<bool> CodeExistsAsync(string code, int? excludeId = null);
        Task AddAsync(PromoCode promoCode);
        Task UpdateAsync(PromoCode promoCode);
        Task DeleteAsync(PromoCode promoCode);
        Task SaveChangesAsync();
    }
}
